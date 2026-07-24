using System;
using System.Runtime.InteropServices;
using extOSC;
using UnityEngine;

// Spike: confirm FMOD's Core/Low-Level API can open a dedicated ASIO System bound to the
// Dante Virtual Soundcard (assumed 12 output channels here) and route a tone to an arbitrary
// channel, independent of Unity's own audio output. Requires the FMOD Unity Integration
// package imported (gives the `FMOD` namespace) - Studio/banks/events are not needed.
//
// Usage: drop on any GameObject, hit Play. It logs available ASIO drivers, binds to
// `asioDriverIndex`, and cycles a sine tone through channels 0..11 every `secondsPerChannel`
// seconds so you can confirm each Dante channel is live with a multimeter/scope/speaker.
public class FmodAsioSpike : MonoBehaviour
{
    [SerializeField] private int asioDriverIndex = 0;
    [SerializeField] private int numChannels = 12;
    [SerializeField] private int sampleRate = 48000;
    [SerializeField] private float toneFrequencyHz = 220f;
    [SerializeField] private float toneGain = 0.5f;
    [Tooltip("If on, automatically cycles through all channels every secondsPerChannel. If off, use the Manual Tile Control buttons in the Inspector instead.")]
    [SerializeField] private bool autoCycle = false;
    [SerializeField] private float secondsPerChannel = 3f;

    [Header("OSC Debug Tap")]
    [Tooltip("Broadcasts the active channel's level over OSC for dante_channel_monitor.py, since Dante blocks a device from self-looping its own Tx back to Rx for real ASIO monitoring.")]
    [SerializeField] private OSCTransmitter debugTransmitter;
    [SerializeField] private string debugOscAddress = "/rr/debug/tile_levels";

    private FMOD.System _system;
    private FMOD.Sound _sound;
    private FMOD.Channel _channel;

    public int NumChannels => numChannels;

    private float _phase;
    private bool[] _channelActive;
    private int _autoCycleChannel = -1;
    private float _channelTimer;

    // FMOD calls this from its mixer thread to pull more samples - must stay allocation-free.
    private FMOD.SOUND_PCMREAD_CALLBACK _pcmReadCallback;

    private void Start()
    {
        _pcmReadCallback = PcmReadCallback;
        _channelActive = new bool[numChannels];

        Check(FMOD.Factory.System_Create(out _system));

        LogAsioDrivers();

        Check(_system.setOutput(FMOD.OUTPUTTYPE.ASIO));
        Check(_system.setDriver(asioDriverIndex));
        Check(_system.setSoftwareFormat(sampleRate, FMOD.SPEAKERMODE.RAW, numChannels));
        Check(_system.init(32, FMOD.INITFLAGS.NORMAL, IntPtr.Zero));

        var exInfo = new FMOD.CREATESOUNDEXINFO();
        exInfo.cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO));
        exInfo.numchannels = 1; // mono source; mix matrix fans it out to the target output channel
        exInfo.defaultfrequency = sampleRate;
        exInfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
        exInfo.pcmreadcallback = _pcmReadCallback;
        exInfo.length = (uint)(sampleRate * sizeof(float)); // ring-buffer size hint, 1s of audio

        Check(_system.createSound(
            string.Empty,
            FMOD.MODE.OPENUSER | FMOD.MODE.LOOP_NORMAL | FMOD.MODE.CREATESTREAM,
            ref exInfo,
            out _sound));

        Check(_system.playSound(_sound, default, false, out _channel));

        SetAutoCycleChannel(0);
        Debug.Log("[RR][INFO] FmodAsioSpike: ASIO system initialized, tone playing.");
    }

    private void Update()
    {
        if (!_system.hasHandle())
        {
            return;
        }

        _system.update();

        if (!autoCycle)
        {
            return;
        }

        _channelTimer += Time.deltaTime;
        if (_channelTimer >= secondsPerChannel)
        {
            _channelTimer = 0f;
            SetAutoCycleChannel((_autoCycleChannel + 1) % numChannels);
        }
    }

    // channelIndex 0..numChannels-1, e.g. tileIndex*2 + (0 = left, 1 = right).
    // Additive: leaves any other already-active channels playing, to demonstrate
    // simultaneous multi-channel ASIO output.
    public void VibrateChannel(int channelIndex)
    {
        if (!_system.hasHandle())
        {
            Debug.LogWarning("[RR][WARN] FmodAsioSpike: system not initialized, enter Play mode first.");
            return;
        }

        if (channelIndex < 0 || channelIndex >= numChannels)
        {
            Debug.LogError($"[RR][ERROR] FmodAsioSpike: channel {channelIndex} out of range (0-{numChannels - 1}).");
            return;
        }

        _channelActive[channelIndex] = true;
        Debug.Log($"[RR][INFO] FmodAsioSpike: channel {channelIndex} now playing.");
        ApplyActiveChannels();
    }

    public void StopAll()
    {
        if (!_system.hasHandle())
        {
            Debug.LogWarning("[RR][WARN] FmodAsioSpike: system not initialized, enter Play mode first.");
            return;
        }

        Array.Clear(_channelActive, 0, _channelActive.Length);
        Debug.Log("[RR][INFO] FmodAsioSpike: stopped, all channels silent.");
        ApplyActiveChannels();
    }

    // Exclusive: used by autoCycle to demonstrate one channel at a time.
    private void SetAutoCycleChannel(int channelIndex)
    {
        _autoCycleChannel = channelIndex;
        Array.Clear(_channelActive, 0, _channelActive.Length);
        _channelActive[channelIndex] = true;
        Debug.Log($"[RR][INFO] FmodAsioSpike: routing tone to channel {channelIndex}.");
        ApplyActiveChannels();
    }

    private void ApplyActiveChannels()
    {
        var matrix = new float[numChannels]; // [out * inchannels + in], inchannels == 1
        for (int i = 0; i < numChannels; i++)
        {
            matrix[i] = _channelActive[i] ? toneGain : 0f;
        }

        Check(_channel.setMixMatrix(matrix, numChannels, 1));
        SendDebugLevels(matrix);
    }

    private void SendDebugLevels(float[] levels)
    {
        if (debugTransmitter == null)
        {
            Debug.LogWarning("[RR][WARN] FmodAsioSpike: no debug OSC transmitter assigned, skipping debug broadcast.");
            return;
        }

        var message = new OSCMessage(debugOscAddress);
        for (int i = 0; i < levels.Length; i++)
        {
            message.AddValue(OSCValue.Float(levels[i]));
        }

        try
        {
            debugTransmitter.Send(message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RR][ERROR] FmodAsioSpike: failed to send debug OSC message - {ex.Message}");
        }
    }

    private FMOD.RESULT PcmReadCallback(IntPtr soundRaw, IntPtr data, uint dataLen)
    {
        int sampleCount = (int)dataLen / sizeof(float);
        float increment = toneFrequencyHz / sampleRate;

        var buffer = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            buffer[i] = Mathf.Sin(_phase * 2f * Mathf.PI);
            _phase += increment;
            if (_phase >= 1f)
            {
                _phase -= 1f;
            }
        }

        Marshal.Copy(buffer, 0, data, sampleCount);
        return FMOD.RESULT.OK;
    }

    private void LogAsioDrivers()
    {
        Check(_system.setOutput(FMOD.OUTPUTTYPE.ASIO));
        Check(_system.getNumDrivers(out int numDrivers));

        Debug.Log($"[RR][INFO] FmodAsioSpike: found {numDrivers} ASIO driver(s).");
        for (int i = 0; i < numDrivers; i++)
        {
            Check(_system.getDriverInfo(i, out string name, 256, out Guid _, out int rate, out FMOD.SPEAKERMODE mode, out int channels));
            Debug.Log($"[RR][INFO] FmodAsioSpike:   [{i}] {name} ({rate} Hz, {channels} ch, {mode})");
        }
    }

    private void OnDestroy()
    {
        if (_sound.hasHandle())
        {
            _sound.release();
        }

        if (_system.hasHandle())
        {
            _system.close();
            _system.release();
        }
    }

    private static void Check(FMOD.RESULT result)
    {
        if (result != FMOD.RESULT.OK)
        {
            Debug.LogError($"[RR][ERROR] FmodAsioSpike: FMOD call failed - {result} ({FMOD.Error.String(result)})");
        }
    }
}
