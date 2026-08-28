using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using extOSC;
using RealityRoost.Shared.Core;
using RealityRoost.Shared.HapticFloor;
using UnityEngine;

namespace RealityRoost.Host.HapticFloor
{
    // Drives the haptic floor directly via FMOD's Core/Low-Level API over ASIO.
    // Has its own FMOD.System bound to the Dante ASIO device for the transducers.
    //
    // Users supply plain Unity AudioClips (via RRHapticEmitter).
    // This driver loads them from Resources, converts them to FMOD.Sound once, and caches the result.
    // Each tile gets its own FMOD.Channel so different tiles can play different clips simultaneously.
    // a new PlayClip on a busy tile interrupts whatever was playing there.
    //
    // HARDWARE NOTE (verified on floor hardware): each tile has exactly ONE addressable
    // ASIO/Dante channel, not a stereo pair. That single channel's signal reaches both of
    // the tile's transducers; the downstream hardware differentiates transducer response
    // based on signal content rather than needing two separate channels. Stereo source
    // clips are therefore downmixed (both input channels summed) into that one output
    // channel so both transducers keep receiving full program content.
    public class HapticFloorDriver : RRSubsystem
    {
        protected override string SubsystemName => "HapticFloorDriver";
        // ASIO device name (Default "Dante Virtual Soundcard")
        [SerializeField] private string asioDeviceName = "Dante Virtual Soundcard";
        [SerializeField] private int sampleRate = 48000;

        [Header("OSC Debug Tap")]
        [Tooltip("Broadcasts per-tile levels over OSC for dante_channel_monitor.py debugger")]
        [SerializeField] private OSCTransmitter debugTransmitter;
        [SerializeField] private string debugOscAddress = "/rr/debug/tile_levels";

        private const int ChannelsPerTile = 1; // one ASIO channel drives both transducers on a tile
        private static readonly int NumChannels = HapticConstants.TILE_COUNT * ChannelsPerTile;

        // Downmix weight applied to each input channel when summing stereo clips down to
        // the tile's single output channel. 0.5 per channel (simple average) matches the
        // downmix used in the Python haptic_floor_tester.py test tool for consistent behavior.
        private const float StereoDownmixWeight = 0.5f;

        private FMOD.System _fmodSystem;
        private readonly FMOD.Channel[] _tileChannels = new FMOD.Channel[HapticConstants.TILE_COUNT];
        private readonly Dictionary<string, CachedClip> _clipCache = new Dictionary<string, CachedClip>();
        private readonly float[] _tileDebugLevels = new float[HapticConstants.TILE_COUNT * ChannelsPerTile];

        private readonly struct CachedClip
        {
            public readonly FMOD.Sound Sound;
            public readonly int Channels;

            public CachedClip(FMOD.Sound sound, int channels)
            {
                Sound = sound;
                Channels = channels;
            }
        }

        protected override void OnSubsystemStart()
        {
            RRNetworkConfig config = RRNetworkConfig.Load();
            if (!config.isHost)
            {
                LogInfo("Not configured as host, skipping HapticFloorDriver initialization.");
                return;
            }

            Check(FMOD.Factory.System_Create(out _fmodSystem));
            Check(_fmodSystem.setOutput(FMOD.OUTPUTTYPE.ASIO));

            if (!TryFindAsioDriver(asioDeviceName, out int driverIndex))
            {
                LogError($"ASIO driver containing '{asioDeviceName}' not found. Haptic Floor will not init. Is Dante Virtual Soundcard installed and running?");
                return;
            }

            Check(_fmodSystem.setDriver(driverIndex));
            Check(_fmodSystem.setSoftwareFormat(sampleRate, FMOD.SPEAKERMODE.RAW, NumChannels));
            Check(_fmodSystem.init(32, FMOD.INITFLAGS.NORMAL, IntPtr.Zero));

            HapticFloorEvents.OnPlayClipRequested += HandlePlayClipRequested;
            HapticFloorEvents.OnRumbleStopped += HandleRumbleStopped;
            LogInfo("haptic floor driver initialized successfully");
        }

        protected override void OnSubsystemStop()
        {
            HapticFloorEvents.OnPlayClipRequested -= HandlePlayClipRequested;
            HapticFloorEvents.OnRumbleStopped -= HandleRumbleStopped;

            for (int tileIndex = 0; tileIndex < HapticConstants.TILE_COUNT; tileIndex++)
            {
                StopTileChannel(tileIndex);
            }

            foreach (CachedClip clip in _clipCache.Values)
            {
                if (clip.Sound.hasHandle())
                {
                    clip.Sound.release();
                }
            }

            _clipCache.Clear();

            if (_fmodSystem.hasHandle())
            {
                _fmodSystem.close();
                _fmodSystem.release();
            }
        }

        private void Update()
        {
            if (!_fmodSystem.hasHandle())
            {
                return;
            }

            _fmodSystem.update();
            PollFinishedChannels();
        }

        private void PollFinishedChannels()
        {
            for (int tileIndex = 0; tileIndex < HapticConstants.TILE_COUNT; tileIndex++)
            {
                FMOD.Channel channel = _tileChannels[tileIndex];
                if (!channel.hasHandle())
                {
                    continue;
                }

                FMOD.RESULT result = channel.isPlaying(out bool isPlaying);
                if (result == FMOD.RESULT.OK && isPlaying)
                {
                    continue;
                }

                _tileChannels[tileIndex] = default;
                _tileDebugLevels[tileIndex] = 0f;
                SendDebugLevels();
            }
        }

        private void HandlePlayClipRequested(int tileIndex, string clipResourcePath, float intensity, bool loop)
        {
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(HandlePlayClipRequested)))
            {
                return;
            }

            intensity = HapticFloorUtils.ClampIntensity(intensity, nameof(HandlePlayClipRequested));
            PlayClipOnTile(tileIndex, clipResourcePath, intensity, loop);
        }

        private void HandleRumbleStopped(int tileIndex)
        {
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(HandleRumbleStopped)))
            {
                return;
            }

            StopTileChannel(tileIndex);
        }

        private void PlayClipOnTile(int tileIndex, string clipResourcePath, float intensity, bool loop)
        {
            if (!_fmodSystem.hasHandle())
            {
                LogWarning($"ASIO system not initialized, ignoring tile {tileIndex}.");
                return;
            }

            // Interrupt: a new request for a tile that's already playing something replaces it
            StopTileChannel(tileIndex);

            if (!TryGetOrLoadClip(clipResourcePath, out CachedClip clip))
            {
                return;
            }

            Check(_fmodSystem.playSound(clip.Sound, default, true, out FMOD.Channel channel));

            float[] matrix = BuildTileMixMatrix(tileIndex, clip.Channels, intensity);
            Check(channel.setMixMatrix(matrix, NumChannels, clip.Channels));
            Check(channel.setMode(loop ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF));
            Check(channel.setLoopCount(loop ? -1 : 0));
            Check(channel.setPaused(false));

            _tileChannels[tileIndex] = channel;

            _tileDebugLevels[tileIndex] = intensity;
            SendDebugLevels();
        }

        private void StopTileChannel(int tileIndex)
        {
            FMOD.Channel channel = _tileChannels[tileIndex];
            if (!channel.hasHandle())
            {
                return;
            }

            FMOD.RESULT result = channel.stop();
            if (result != FMOD.RESULT.OK && result != FMOD.RESULT.ERR_INVALID_HANDLE)
            {
                LogError($"FMOD call failed - {result} ({FMOD.Error.String(result)})");
            }

            _tileChannels[tileIndex] = default;

            _tileDebugLevels[tileIndex] = 0f;
            SendDebugLevels();
        }

        private void SendDebugLevels()
        {
            if (debugTransmitter == null)
            {
                LogError("Debug OSC Transmitter not assigned in inspector!");
                return;
            }

            var message = new OSCMessage(debugOscAddress);
            for (int i = 0; i < _tileDebugLevels.Length; i++)
            {
                message.AddValue(OSCValue.Float(_tileDebugLevels[i]));
            }

            try
            {
                debugTransmitter.Send(message);
            }
            catch (Exception ex)
            {
                LogError($"Failed to send debug OSC message - {ex.Message}");
            }
        }

        // Builds an FMOD mix matrix routing a clip's input channels down to this tile's
        // single output channel. Mono clips pass through at full gain. Stereo clips are
        // downmixed (both input channels summed at StereoDownmixWeight each) so the one
        // physical channel still carries full program content for both transducers.
        private static float[] BuildTileMixMatrix(int tileIndex, int inChannels, float gain)
        {
            var matrix = new float[NumChannels * inChannels];
            int outChannel = tileIndex; // one output channel per tile

            if (inChannels == 1)
            {
                matrix[outChannel * inChannels] = gain;
            }
            else
            {
                // downmix: sum all input channels into the single output channel.
                // Only the first two input channels are used even if the source has more.
                for (int inCh = 0; inCh < Math.Min(inChannels, 2); inCh++)
                {
                    matrix[outChannel * inChannels + inCh] = gain * StereoDownmixWeight;
                }
            }

            return matrix;
        }

        private bool TryGetOrLoadClip(string clipResourcePath, out CachedClip clip)
        {
            if (_clipCache.TryGetValue(clipResourcePath, out clip))
            {
                return true;
            }

            AudioClip unityClip = Resources.Load<AudioClip>(clipResourcePath);
            if (unityClip == null)
            {
                LogError($"AudioClip not found at Resources/{clipResourcePath}.");
                clip = default;
                return false;
            }

            var samples = new float[unityClip.samples * unityClip.channels];
            unityClip.GetData(samples, 0);

            var bytes = new byte[samples.Length * sizeof(float)];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);

            var exInfo = new FMOD.CREATESOUNDEXINFO
            {
                cbsize = Marshal.SizeOf(typeof(FMOD.CREATESOUNDEXINFO)),
                numchannels = unityClip.channels,
                defaultfrequency = unityClip.frequency,
                format = FMOD.SOUND_FORMAT.PCMFLOAT,
                length = (uint)bytes.Length,
            };

            FMOD.RESULT result = _fmodSystem.createSound(
                bytes,
                FMOD.MODE.OPENMEMORY | FMOD.MODE.OPENRAW | FMOD.MODE.CREATESAMPLE,
                ref exInfo,
                out FMOD.Sound sound);

            Check(result);
            if (result != FMOD.RESULT.OK)
            {
                clip = default;
                return false;
            }

            clip = new CachedClip(sound, unityClip.channels);
            _clipCache[clipResourcePath] = clip;
            return true;
        }

        private bool TryFindAsioDriver(string deviceName, out int driverIndex)
        {
            Check(_fmodSystem.getNumDrivers(out int numDrivers));

            for (int i = 0; i < numDrivers; i++)
            {
                Check(_fmodSystem.getDriverInfo(i, out string name, 256, out Guid _, out int _, out FMOD.SPEAKERMODE _, out int _));
                LogDebug($"found asio device {name} @ index {i}");
                if (name.Contains(deviceName))
                {
                    driverIndex = i;
                    LogDebug($"desired device found!! using asio device {name} @ index {i}");
                    return true;
                }
            }

            driverIndex = -1;
            return false;
        }

        private void Check(FMOD.RESULT result)
        {
            if (result != FMOD.RESULT.OK)
            {
                LogError($"FMOD call failed - {result} ({FMOD.Error.String(result)})");
            }
        }
    }
}