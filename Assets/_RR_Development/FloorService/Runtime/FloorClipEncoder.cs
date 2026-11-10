using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RealityRoost.Shared.HapticFloor
{
    /// <summary>
    /// Converts a Unity AudioClip to WAV data so Floor Software can play it.
    /// </summary>
    public static class FloorClipEncoder
    {
        // Mono PCM16 keeps uploads small and matches the floor service's mono playback.
        public static byte[] Encode(AudioClip clip)
        {
            if (clip == null) throw new ArgumentException("Assign a Unity AudioClip.");
            if (clip.loadType != AudioClipLoadType.DecompressOnLoad || clip.loadState != AudioDataLoadState.Loaded)
                throw new InvalidOperationException("Set the AudioClip Load Type to Decompress On Load, enable Preload Audio Data, and wait for it to load.");
            long size = 44L + clip.samples * 2L;
            if (size > 25 * 1024 * 1024) throw new InvalidOperationException("The decoded mono WAV exceeds the Floor Software's 25 MiB limit. Use a shorter clip.");
            var pcm = new float[clip.samples * clip.channels];
            if (!clip.GetData(pcm, 0)) throw new InvalidOperationException("Cannot read AudioClip data. Use Decompress On Load in its import settings.");
            using (var stream = new MemoryStream((int)size))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write((int)size - 8);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(clip.frequency);
                writer.Write(clip.frequency * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(clip.samples * 2);
                for (int frame = 0; frame < clip.samples; frame++)
                {
                    double sum = 0;
                    for (int channel = 0; channel < clip.channels; channel++) sum += pcm[frame * clip.channels + channel];
                    float value = (float)(sum / clip.channels);
                    if (float.IsNaN(value) || float.IsInfinity(value)) value = 0;
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value, -1, 1) * 32767));
                }
                return stream.ToArray();
            }
        }
    }
}
