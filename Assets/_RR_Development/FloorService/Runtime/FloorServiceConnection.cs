using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RealityRoost.Shared.HapticFloor
{
    [Flags]
    public enum FloorTiles
    {
        None = 0, Tile1 = 1, Tile2 = 2, Tile3 = 4, Tile4 = 8, Tile5 = 16, Tile6 = 32,
        All = 63
    }
    [Serializable] public class FloorAudioStatus { public bool ready; public string device; public string error; }
    [Serializable] public class FloorTileStatus { public int tile_index; public bool playing; public string sound_id; public float intensity; public bool looping; }
    [Serializable] public class FloorServiceStatus { public int protocol_version; public FloorAudioStatus audio; public FloorTileStatus[] tiles; }
    [Serializable] public class FloorSound { public string sound_id; public float duration_seconds; }
    [Serializable] public class FloorSoundCatalog { public FloorSound[] sounds; }
    [Serializable] public class FloorAssignment { public int tile_index; public string sound_id; public float intensity; public bool loop; }
    [Serializable] internal class FloorPlayRequest { public FloorAssignment[] assignments; }
    [Serializable] internal class FloorUploadResponse { public string sound_id; }

    // HTTP only: the external service owns sounds, ASIO and hardware. Tick on Unity's main thread.
    /// <summary>
    /// Sends HTTP requests to Floor Software to check tile status, upload sounds, and start or stop playback.
    /// </summary>
    public sealed class FloorServiceConnection : IDisposable
    {
        private class Command { public string path; public string json; public byte[] wav; public string key; public FloorTiles tiles; public float intensity; public bool loop; }
        private readonly Queue<Command> commands = new Queue<Command>();
        private readonly Dictionary<string, string> uploadedSounds = new Dictionary<string, string>();
        private Command uploading;
        private UnityWebRequest poll, command, catalog;
        private double nextPoll;
        private string endpoint;
        public FloorServiceStatus Status { get; private set; }
        public FloorSound[] Sounds { get; private set; } = Array.Empty<FloorSound>();
        public string ConnectionError { get; private set; } = "Waiting for service";
        public string CommandResult { get; private set; } = "No command sent";
        public double LastStatusTime { get; private set; } = double.NegativeInfinity;
        public bool IsFresh => Status != null && Time.realtimeSinceStartupAsDouble - LastStatusTime < 3;
        public bool IsBusy => command != null || commands.Count != 0;
        public int FailureVersion { get; private set; }
        public bool TryGetUploadedSound(string key, out string id) => uploadedSounds.TryGetValue(key, out id);

        public string PrepareWav(byte[] wav)
        {
            if (wav == null || wav.Length < 44 || wav.Length > 25 * 1024 * 1024)
                throw new ArgumentException("Audio must be a WAV of at most 25 MiB.");
            string key;
            using (var hash = System.Security.Cryptography.SHA256.Create())
                key = BitConverter.ToString(hash.ComputeHash(wav)).Replace("-", "").ToLowerInvariant();
            if (uploadedSounds.ContainsKey(key)) return key;
            if (commands.Count >= 32) throw new InvalidOperationException("Command queue full.");
            commands.Enqueue(new Command { wav = wav, key = key });
            return key;
        }

        public void Tick(string url)
        {
            url = (url ?? "").TrimEnd('/');
            if (url != endpoint) { Dispose(); endpoint = url; }
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            { ConnectionError = "Set an absolute HTTP service URL."; return; }
            if (poll != null && poll.isDone)
            {
                try
                {
                    if (poll.result != UnityWebRequest.Result.Success) throw new Exception(poll.error);
                    var value = JsonUtility.FromJson<FloorServiceStatus>(poll.downloadHandler.text);
                    if (value == null || value.protocol_version != 1 || value.audio == null || value.tiles == null || value.tiles.Length != 6)
                        throw new Exception("Unsupported floor status response");
                    var seen = new HashSet<int>();
                    foreach (var tile in value.tiles)
                        if (tile == null || tile.tile_index < 0 || tile.tile_index > 5 || !seen.Add(tile.tile_index))
                            throw new Exception("Invalid tile status response");
                    Status = value;
                    LastStatusTime = Time.realtimeSinceStartupAsDouble;
                    ConnectionError = "";
                }
                catch (Exception e) { Status = null; ConnectionError = e.Message; }
                poll.Dispose(); poll = null;
            }
            if (catalog != null && catalog.isDone)
            {
                try
                {
                    if (catalog.result != UnityWebRequest.Result.Success) throw new Exception(catalog.error);
                    Sounds = JsonUtility.FromJson<FloorSoundCatalog>(catalog.downloadHandler.text)?.sounds ?? Array.Empty<FloorSound>();
                }
                catch (Exception e) { CommandResult = "Catalog failed: " + e.Message; }
                catalog.Dispose(); catalog = null;
            }
            if (command != null && command.isDone)
            {
                if (command.result != UnityWebRequest.Result.Success) FailureVersion++;
                CommandResult = command.result == UnityWebRequest.Result.Success ? "Command accepted by service" :
                    $"Command failed ({command.responseCode}): {command.error} {command.downloadHandler.text}";
                if (uploading != null)
                {
                    bool success = false;
                    try
                    {
                        if (command.result != UnityWebRequest.Result.Success) throw new Exception(CommandResult);
                        string id = JsonUtility.FromJson<FloorUploadResponse>(command.downloadHandler.text)?.sound_id;
                        if (string.IsNullOrWhiteSpace(id)) throw new Exception("Upload response has no sound ID.");
                        uploadedSounds[uploading.key] = id;
                        success = true;
                    }
                    catch (Exception e) { FailureVersion++; CommandResult = "Audio upload failed: " + e.Message; }
                    if (!success && commands.Count > 0 && ReferenceEquals(commands.Peek(), uploading)) commands.Dequeue();
                    uploading = null;
                }
                command.Dispose(); command = null; nextPoll = 0;
            }
            if (command == null && commands.Count > 0)
            {
                var item = commands.Peek();
                if (item.wav != null && !uploadedSounds.ContainsKey(item.key))
                {
                    var form = new WWWForm();
                    form.AddField("experience_id", "unity");
                    form.AddField("name", item.key);
                    form.AddBinaryData("file", item.wav, item.key + ".wav", "audio/wav");
                    command = UnityWebRequest.Post(endpoint + "/v1/sounds", form);
                    command.timeout = 60;
                    uploading = item;
                    CommandResult = "Uploading Unity audio…";
                    command.SendWebRequest();
                    return;
                }
                commands.Dequeue();
                // Preload-only requests never trigger hardware playback.
                if (item.path == null) return;
                if (item.wav != null) item.json = PlayJson(item.tiles, uploadedSounds[item.key], item.intensity, item.loop);
                command = new UnityWebRequest(endpoint + item.path, "POST") { downloadHandler = new DownloadHandlerBuffer(), timeout = 3 };
                if (item.json != null)
                {
                    command.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(item.json));
                    command.SetRequestHeader("Content-Type", "application/json");
                }
                command.SendWebRequest();
            }
            if (poll == null && Time.realtimeSinceStartupAsDouble >= nextPoll)
            {
                poll = UnityWebRequest.Get(endpoint + "/v1/status"); poll.timeout = 2; poll.SendWebRequest();
                nextPoll = Time.realtimeSinceStartupAsDouble + 0.5;
            }
        }

        public void RefreshCatalog()
        {
            if (catalog != null || string.IsNullOrEmpty(endpoint)) return;
            catalog = UnityWebRequest.Get(endpoint + "/v1/sounds"); catalog.timeout = 3; catalog.SendWebRequest();
        }

        public void Play(int tile, string sound, float intensity, bool loop)
        {
            if (tile < 0 || tile > 5)
            { CommandResult = "Choose tile 0–5."; return; }
            PlayGroup((FloorTiles)(1 << tile), sound, intensity, loop);
        }

        public void PlayAll(string sound, float intensity, bool loop)
        { PlayGroup(FloorTiles.All, sound, intensity, loop); }

        public void PlayGroup(FloorTiles tiles, string sound, float intensity, bool loop)
        {
            if (tiles == FloorTiles.None || (tiles & ~FloorTiles.All) != 0 || string.IsNullOrWhiteSpace(sound) || sound.Length > 512 ||
                float.IsNaN(intensity) || intensity <= 0 || intensity > 1)
            { CommandResult = "Choose tiles 1–6, a sound ID and intensity greater than 0 up to 1."; return; }
            Enqueue("/v1/play", PlayJson(tiles, sound, intensity, loop));
        }

        private static string PlayJson(FloorTiles tiles, string sound, float intensity, bool loop)
        {
            var assignments = new List<FloorAssignment>();
            for (int tile = 0; tile < 6; tile++)
                if (((int)tiles & (1 << tile)) != 0)
                    assignments.Add(new FloorAssignment { tile_index = tile, sound_id = sound, intensity = intensity, loop = loop });
            // One batch request starts the selected tiles together in the external service.
            return JsonUtility.ToJson(new FloorPlayRequest { assignments = assignments.ToArray() });
        }

        public void PlayWav(FloorTiles tiles, byte[] wav, float intensity, bool loop)
        {
            if (tiles == FloorTiles.None || (tiles & ~FloorTiles.All) != 0 || float.IsNaN(intensity) || intensity <= 0 || intensity > 1)
                throw new ArgumentException("Choose tiles and intensity greater than zero up to one.");
            if (wav == null || wav.Length < 44 || wav.Length > 25 * 1024 * 1024)
                throw new ArgumentException("Audio must be a WAV of at most 25 MiB.");
            if (commands.Count >= 32) { CommandResult = "Command queue full; request rejected."; return; }
            string key;
            using (var hash = System.Security.Cryptography.SHA256.Create())
                key = BitConverter.ToString(hash.ComputeHash(wav)).Replace("-", "").ToLowerInvariant();
            commands.Enqueue(new Command { path = "/v1/play", wav = wav, key = key, tiles = tiles, intensity = intensity, loop = loop });
            CommandResult = "Unity audio queued";
        }

        public void StopGroup(FloorTiles tiles)
        {
            if (tiles == FloorTiles.None || (tiles & ~FloorTiles.All) != 0)
            { CommandResult = "Choose tiles 1–6."; return; }
            for (int tile = 0; tile < 6; tile++)
                if (((int)tiles & (1 << tile)) != 0) Stop(tile);
        }

        public void Stop(int tile)
        {
            if (tile < 0 || tile >= 6) return;
            // A stop must also cancel that tile's playback waiting for an upload.
            int count = commands.Count;
            for (int i = 0; i < count; i++)
            {
                var pending = commands.Dequeue();
                if (pending.wav != null)
                {
                    pending.tiles &= ~(FloorTiles)(1 << tile);
                    if (pending.tiles == FloorTiles.None) continue;
                }
                commands.Enqueue(pending);
            }
            Enqueue($"/v1/tiles/{tile}/stop", null);
        }

        public void StopAll()
        {
            commands.Clear();
            for (int tile = 0; tile < 6; tile++) Stop(tile);
        }

        private void Enqueue(string path, string json)
        {
            if (commands.Count >= 32) { CommandResult = "Command queue full; request rejected."; return; }
            commands.Enqueue(new Command { path = path, json = json });
            CommandResult = "Command queued";
        }

        public void Dispose()
        {
            poll?.Abort(); poll?.Dispose(); poll = null;
            command?.Abort(); command?.Dispose(); command = null;
            catalog?.Abort(); catalog?.Dispose(); catalog = null;
            commands.Clear(); uploadedSounds.Clear(); uploading = null; Status = null; Sounds = Array.Empty<FloorSound>(); nextPoll = 0;
            LastStatusTime = double.NegativeInfinity; ConnectionError = "Waiting for service";
        }
    }
}
