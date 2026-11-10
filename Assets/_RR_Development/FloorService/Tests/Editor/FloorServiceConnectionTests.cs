using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using RealityRoost.Shared.HapticFloor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests floor playback and communication using a local test server instead of physical tiles.
/// </summary>
public class FloorServiceConnectionTests
{
    [UnityTest] public IEnumerator SequenceRateChangesPreserveProgressAndPause()
    {
        yield return Until(() => connection.IsFresh);
        var preset = ScriptableObject.CreateInstance<FloorSequence>();
        var player = new FloorSequencePlayer();
        try
        {
            preset.steps[1].time = 1; preset.steps[2].time = 2; preset.cycleDuration = 4;
            player.Start(preset, connection, null, "roost/train", 2);
            player.Tick(connection, 0);
            yield return Until(() => !connection.IsBusy);
            Assert.AreEqual(1, commands.Count);
            player.Tick(connection, .49); Assert.IsFalse(connection.IsBusy);
            player.Tick(connection, .5);
            yield return Until(() => !connection.IsBusy);
            Assert.AreEqual(2, commands.Count);
            player.SetPlaybackRate(.5f, .5);
            player.Tick(connection, 2.49); Assert.IsFalse(connection.IsBusy);
            player.Tick(connection, 2.5);
            yield return Until(() => !connection.IsBusy);
            Assert.AreEqual(3, commands.Count);
            player.SetPlaybackRate(0, 2.5);
            player.Tick(connection, 20); Assert.IsFalse(connection.IsBusy);
            Assert.AreEqual(3, commands.Count);
            player.SetPlaybackRate(2, 20);
            player.Tick(connection, 20.99); Assert.IsFalse(connection.IsBusy);
            player.Tick(connection, 21);
            yield return Until(() => !connection.IsBusy);
            Assert.AreEqual(4, commands.Count);
            Assert.AreEqual(4, preset.cycleDuration, "Rate changes must not edit the preset asset.");
        }
        finally { player.Cancel(); UnityEngine.Object.DestroyImmediate(preset); }
    }

    [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(9f)]
    public void InvalidPlaybackRatesAreRejected(float rate)
    {
        Assert.Throws<ArgumentException>(() => new FloorSequencePlayer().SetPlaybackRate(rate, 0));
    }
    [Test] public void TrainPresetImportsWithReadablePulse()
    {
        var preset = UnityEditor.AssetDatabase.LoadAssetAtPath<FloorSequence>("Assets/_RR_Development/FloorService/Presets/Train Rolling.asset");
        Assert.IsNotNull(preset);
        preset.Validate();
        Assert.IsNotNull(preset.audioClip);
        Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, preset.audioClip.loadType);
        Assert.Greater(FloorClipEncoder.Encode(preset.audioClip).Length, 44);
        Assert.AreEqual(1.5f, preset.cycleDuration);
        CollectionAssert.AreEqual(new[] { 3, 12, 48 }, Array.ConvertAll(preset.steps, s => (int)s.tiles));
    }

    [UnityTest] public IEnumerator SequenceDoesNotBurstAfterStall()
    {
        yield return Until(() => connection.IsFresh);
        var preset = ScriptableObject.CreateInstance<FloorSequence>();
        var player = new FloorSequencePlayer();
        try
        {
            player.Start(preset, connection, null, "roost/train");
            player.Tick(connection, 0);
            yield return Until(() => !connection.IsBusy);
            player.Tick(connection, 2);
            yield return Until(() => !connection.IsBusy);
            player.Tick(connection, 2.01);
            Assert.IsFalse(connection.IsBusy);
            Assert.AreEqual(2, commands.Count);
            player.Stop(connection);
        }
        finally { UnityEngine.Object.DestroyImmediate(preset); }
    }

    [UnityTest] public IEnumerator SequenceRunsRowsInOrderAndRepeats()
    {
        yield return Until(() => connection.IsFresh);
        var preset = ScriptableObject.CreateInstance<FloorSequence>();
        var player = new FloorSequencePlayer();
        try
        {
            player.Start(preset, connection, null, "roost/train");
            double[] times = { 0, .151, .302, 1.503 };
            int[][] expected = { new[] { 0, 1 }, new[] { 2, 3 }, new[] { 4, 5 }, new[] { 0, 1 } };
            for (int i = 0; i < times.Length; i++)
            {
                player.Tick(connection, times[i]);
                yield return Until(() => !connection.IsBusy);
                Assert.AreEqual(1, commands.Count);
                Assert.IsTrue(commands.TryDequeue(out var request));
                var batch = JsonUtility.FromJson<Batch>(request.Substring("/v1/play ".Length));
                CollectionAssert.AreEqual(expected[i], Array.ConvertAll(batch.assignments, a => a.tile_index));
                Assert.IsFalse(batch.assignments[0].loop);
                player.Tick(connection, times[i] + .05);
                Assert.IsFalse(connection.IsBusy, "Next row must wait for its offset");
            }
            player.Stop(connection);
            yield return Until(() => !connection.IsBusy);
            Assert.AreEqual(6, commands.Count);
            player.Tick(connection, 20);
            Assert.IsFalse(connection.IsBusy);
        }
        finally { UnityEngine.Object.DestroyImmediate(preset); }
    }

    [UnityTest] public IEnumerator SequencePreloadsWithoutPlayingAndCanCancel()
    {
        yield return Until(() => connection.IsFresh);
        var preset = ScriptableObject.CreateInstance<FloorSequence>();
        var player = new FloorSequencePlayer();
        try
        {
            player.Start(preset, connection, TestWav(), null);
            player.Tick(connection, 0);
            yield return Until(() => !connection.IsBusy);
            Assert.AreEqual(1, commands.Count);
            Assert.IsTrue(commands.TryDequeue(out var upload));
            StringAssert.StartsWith("/v1/sounds ", upload);
            player.Stop(connection);
            yield return Until(() => !connection.IsBusy);
            player.Tick(connection, 10);
            foreach (var request in commands) Assert.IsFalse(request.StartsWith("/v1/play "));
        }
        finally { UnityEngine.Object.DestroyImmediate(preset); }
    }

    [Test] public void InvalidSequenceTimingIsRejected()
    {
        var preset = ScriptableObject.CreateInstance<FloorSequence>();
        try
        {
            preset.steps[1].time = 0;
            Assert.Throws<ArgumentException>(() => preset.Validate());
            preset.steps[1].time = .15f;
            preset.cycleDuration = .2f;
            Assert.Throws<ArgumentException>(() => preset.Validate());
        }
        finally { UnityEngine.Object.DestroyImmediate(preset); }
    }

    [Serializable] private class Batch { public FloorAssignment[] assignments; }
    private HttpListener listener;
    private FloorServiceConnection connection;
    private string url;
    private readonly ConcurrentQueue<string> commands = new ConcurrentQueue<string>();
    private volatile string status;
    private volatile bool reject;

    [SetUp] public void SetUp()
    {
        var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start(); int port = ((IPEndPoint)socket.LocalEndpoint).Port; socket.Stop();
        url = "http://127.0.0.1:" + port;
        listener = new HttpListener(); listener.Prefixes.Add(url + "/"); listener.Start();
        connection = new FloorServiceConnection(); reject = false;
        status = "{\"protocol_version\":1,\"audio\":{\"ready\":true},\"tiles\":[";
        for (int i = 0; i < 6; i++) status += (i == 0 ? "" : ",") + "{\"tile_index\":" + i + ",\"playing\":true,\"sound_id\":\"custom/test/impact@123\",\"intensity\":0.5,\"looping\":true}";
        status += "]}";
        _ = Serve();
    }

    private async Task Serve()
    {
        try
        {
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                string body = status;
                if (context.Request.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(context.Request.InputStream))
                        commands.Enqueue(context.Request.Url.AbsolutePath + " " + await reader.ReadToEndAsync());
                    context.Response.StatusCode = reject ? 503 : 200;
                    body = reject ? "{\"error\":{\"message\":\"ASIO is not ready\"}}" : "{\"accepted\":true}";
                    if (!reject && context.Request.Url.AbsolutePath == "/v1/sounds")
                        body = "{\"sound_id\":\"custom/unity/uploaded@123\"}";
                }
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                context.Response.Close();
            }
        }
        catch (HttpListenerException) { }
        catch (ObjectDisposedException) { }
    }

    [TearDown] public void TearDown() { connection.Dispose(); listener.Close(); while (commands.TryDequeue(out _)) { } }

    private IEnumerator Until(Func<bool> condition)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while (!condition() && Time.realtimeSinceStartupAsDouble < deadline)
        { connection.Tick(url); yield return null; }
        Assert.IsTrue(condition(), "Timed out waiting for loopback service");
    }

    [UnityTest] public IEnumerator PlayAndStopUseServiceContract()
    {
        yield return Until(() => connection.IsFresh);
        Assert.IsTrue(connection.Status.tiles[0].looping);
        connection.Play(2, "custom/test/impact@123", .5f, true);
        connection.Stop(2);
        yield return Until(() => commands.Count == 2 && connection.CommandResult == "Command accepted by service");
        Assert.IsTrue(commands.TryDequeue(out var play));
        StringAssert.StartsWith("/v1/play ", play);
        StringAssert.Contains("\"tile_index\":2", play);
        StringAssert.Contains("\"sound_id\":\"custom/test/impact@123\"", play);
        StringAssert.Contains("\"loop\":true", play);
        Assert.IsTrue(commands.TryDequeue(out var stop));
        Assert.AreEqual("/v1/tiles/2/stop ", stop);
    }

    private static byte[] TestWav()
    {
        var clip = AudioClip.Create("Test", 480, 2, 48000, false);
        try
        {
            var samples = new float[960];
            for (int i = 0; i < samples.Length; i += 2) { samples[i] = .6f; samples[i + 1] = .2f; }
            clip.SetData(samples, 0);
            return FloorClipEncoder.Encode(clip);
        }
        finally { UnityEngine.Object.DestroyImmediate(clip); }
    }

    [Test] public void EncoderWritesMonoPcmWav()
    {
        var wav = TestWav();
        Assert.AreEqual("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.AreEqual(1004, wav.Length);
        Assert.AreEqual(1, BitConverter.ToInt16(wav, 22));
        Assert.AreEqual(48000, BitConverter.ToInt32(wav, 24));
        Assert.AreEqual(16, BitConverter.ToInt16(wav, 34));
        Assert.That(BitConverter.ToInt16(wav, 44), Is.EqualTo(13107).Within(1));
    }

    [UnityTest] public IEnumerator UploadPrecedesPlayAndIsReused()
    {
        yield return Until(() => connection.IsFresh);
        var wav = TestWav();
        connection.PlayWav(FloorTiles.All, wav, .5f, false);
        yield return Until(() => commands.Count == 2 && connection.CommandResult == "Command accepted by service");
        Assert.IsTrue(commands.TryDequeue(out var upload));
        StringAssert.StartsWith("/v1/sounds ", upload);
        StringAssert.Contains("experience_id", upload);
        StringAssert.Contains("audio/wav", upload);
        Assert.IsTrue(commands.TryDequeue(out var play));
        StringAssert.Contains("custom/unity/uploaded@123", play);
        connection.PlayWav(FloorTiles.Tile1, wav, .5f, false);
        yield return Until(() => commands.Count == 1 && connection.CommandResult == "Command accepted by service");
        Assert.IsTrue(commands.TryDequeue(out var again));
        StringAssert.StartsWith("/v1/play ", again);
    }

    [UnityTest] public IEnumerator UploadFailureDoesNotPlay()
    {
        yield return Until(() => connection.IsFresh);
        reject = true;
        connection.PlayWav(FloorTiles.Tile1, TestWav(), .5f, false);
        yield return Until(() => connection.CommandResult.StartsWith("Audio upload failed"));
        Assert.AreEqual(1, commands.Count);
        Assert.IsTrue(commands.TryDequeue(out var upload));
        StringAssert.StartsWith("/v1/sounds ", upload);
    }

    [UnityTest] public IEnumerator StopAllDuringUploadDoesNotPlay()
    {
        yield return Until(() => connection.IsFresh);
        connection.PlayWav(FloorTiles.All, TestWav(), .5f, true);
        connection.Tick(url);
        connection.StopAll();
        yield return Until(() => commands.Count == 7);
        foreach (var request in commands) Assert.IsFalse(request.StartsWith("/v1/play "));
    }

    [UnityTest] public IEnumerator StopDuringUploadRemovesOnlyStoppedTile()
    {
        yield return Until(() => connection.IsFresh);
        connection.PlayWav(FloorTiles.Tile1 | FloorTiles.Tile2, TestWav(), .5f, true);
        connection.Tick(url);
        connection.Stop(0);
        yield return Until(() => commands.Count == 3);
        Assert.IsTrue(commands.TryDequeue(out _));
        Assert.IsTrue(commands.TryDequeue(out var play));
        var batch = JsonUtility.FromJson<Batch>(play.Substring("/v1/play ".Length));
        Assert.AreEqual(1, batch.assignments.Length);
        Assert.AreEqual(1, batch.assignments[0].tile_index);
    }

    [UnityTest] public IEnumerator InvalidStatusBecomesUnknown()
    {
        yield return Until(() => connection.IsFresh);
        status = "{\"protocol_version\":1}";
        yield return Until(() => !connection.IsFresh);
        Assert.IsNull(connection.Status);
        StringAssert.Contains("Unsupported", connection.ConnectionError);
    }

    [UnityTest] public IEnumerator RejectedPlaybackIsReported()
    {
        yield return Until(() => connection.IsFresh);
        reject = true;
        connection.Play(0, "custom/test/impact@123", .5f, false);
        yield return Until(() => connection.CommandResult.Contains("503"));
        StringAssert.Contains("ASIO is not ready", connection.CommandResult);
    }

    [UnityTest] public IEnumerator StopAllDropsQueuedPlayback()
    {
        yield return Until(() => connection.IsFresh);
        connection.Play(0, "custom/test/impact@123", .5f, true);
        connection.StopAll();
        yield return Until(() => commands.Count == 6);
        for (int i = 0; i < 6; i++)
        {
            Assert.IsTrue(commands.TryDequeue(out var stop));
            Assert.AreEqual($"/v1/tiles/{i}/stop ", stop);
        }
    }

    [UnityTest] public IEnumerator GroupPlaybackUsesOneBatch()
    { yield return CheckBatch(FloorTiles.Tile1 | FloorTiles.Tile3 | FloorTiles.Tile6, new[] { 0, 2, 5 }); }

    [UnityTest] public IEnumerator AllPlaybackUsesOneBatch()
    { yield return CheckBatch(FloorTiles.All, new[] { 0, 1, 2, 3, 4, 5 }); }

    private IEnumerator CheckBatch(FloorTiles tiles, int[] expected)
    {
        yield return Until(() => connection.IsFresh);
        if (tiles == FloorTiles.All) connection.PlayAll("custom/test/impact@123", .4f, true);
        else connection.PlayGroup(tiles, "custom/test/impact@123", .4f, true);
        yield return Until(() => connection.CommandResult == "Command accepted by service");
        Assert.AreEqual(1, commands.Count, "Group playback must be a single HTTP request");
        Assert.IsTrue(commands.TryDequeue(out var request));
        StringAssert.StartsWith("/v1/play ", request);
        var batch = JsonUtility.FromJson<Batch>(request.Substring("/v1/play ".Length));
        Assert.AreEqual(expected.Length, batch.assignments.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i], batch.assignments[i].tile_index);
            Assert.AreEqual("custom/test/impact@123", batch.assignments[i].sound_id);
            Assert.AreEqual(.4f, batch.assignments[i].intensity);
            Assert.IsTrue(batch.assignments[i].loop);
        }
    }

    [UnityTest] public IEnumerator StopGroupOnlyStopsSelectedTiles()
    {
        yield return Until(() => connection.IsFresh);
        connection.StopGroup(FloorTiles.Tile2 | FloorTiles.Tile5);
        yield return Until(() => commands.Count == 2);
        Assert.IsTrue(commands.TryDequeue(out var first));
        Assert.IsTrue(commands.TryDequeue(out var second));
        Assert.AreEqual("/v1/tiles/1/stop ", first);
        Assert.AreEqual("/v1/tiles/4/stop ", second);
    }

    [TestCase(0)] [TestCase(64)] [TestCase(-1)]
    public void InvalidGroupIsRejected(int mask)
    {
        connection.PlayGroup((FloorTiles)mask, "custom/test/impact@123", .5f, false);
        StringAssert.Contains("Choose tiles", connection.CommandResult);
        Assert.IsEmpty(commands);
    }

    [TestCase(-1, .5f)]
    [TestCase(6, .5f)]
    [TestCase(0, 0f)]
    [TestCase(0, float.NaN)]
    [TestCase(0, float.PositiveInfinity)]
    public void InvalidPlayIsRejected(int tile, float gain)
    {
        connection.Play(tile, "custom/test/impact@123", gain, false);
        StringAssert.Contains("Choose tile", connection.CommandResult);
        Assert.IsEmpty(commands);
    }
}
