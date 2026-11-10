using System;

namespace RealityRoost.Shared.HapticFloor
{
    // Driven by the host/editor clock. HTTP delivery is approximate, not sample-scheduled.
    /// <summary>
    /// Schedules the tile groups in a preset and adjusts their timing when the playback rate changes.
    /// </summary>
    public sealed class FloorSequencePlayer
    {
        private FloorSequenceStep[] steps;
        private bool repeat;
        private float cycle;
        private string key, sound;
        private int index, failures;
        private double start = double.NaN;
        private double waitingSince = double.NaN;
        private double clock, lastTime = double.NaN;
        public float PlaybackRate { get; private set; } = 1;

        public void SetPlaybackRate(float rate, double now)
        {
            ValidateRate(rate);
            AdvanceClock(now);
            PlaybackRate = rate;
        }

        public static void ValidateRate(float rate)
        {
            if (float.IsNaN(rate) || float.IsInfinity(rate) || rate < 0 || rate > 8)
                throw new ArgumentException("Playback rate must be between zero and eight.");
        }

        private void AdvanceClock(double now)
        {
            if (!double.IsNaN(lastTime)) clock += Math.Max(0, now - lastTime) * PlaybackRate;
            lastTime = now;
        }
        public bool IsRunning { get; private set; }
        public string Status { get; private set; } = "No sequence running";

        public void Start(FloorSequence preset, FloorServiceConnection connection, byte[] wav, string soundId, float rate = 1)
        {
            preset.Validate();
            ValidateRate(rate);
            clock = 0; lastTime = double.NaN; PlaybackRate = rate;
            if (wav == null && string.IsNullOrWhiteSpace(soundId)) throw new ArgumentException("Assign an AudioClip or Sound Id.");
            steps = Array.ConvertAll(preset.steps, s => new FloorSequenceStep { time = s.time, tiles = s.tiles, intensity = s.intensity });
            repeat = preset.repeat; cycle = preset.cycleDuration;
            failures = connection.FailureVersion;
            key = wav == null ? null : connection.PrepareWav(wav);
            sound = soundId; index = 0; start = double.NaN; waitingSince = double.NaN; IsRunning = true;
            Status = "Preparing sequence audio";
        }

        public void Tick(FloorServiceConnection connection, double now)
        {
            if (!IsRunning) return;
            AdvanceClock(now);
            if (double.IsNaN(waitingSince)) waitingSince = now;
            if (connection.FailureVersion != failures)
            { Stop(connection); Status = "Sequence stopped: service command failed"; return; }
            if (!connection.IsFresh || !connection.Status.audio.ready)
            {
                if (double.IsNaN(start) && now - waitingSince < 5) return;
                Stop(connection); Status = "Sequence stopped: floor status unavailable"; return;
            }
            if (key != null && !connection.TryGetUploadedSound(key, out sound)) return;
            if (connection.IsBusy) return;
            if (PlaybackRate == 0) return;
            if (double.IsNaN(start)) start = clock;
            double due = start + steps[index].time;
            if (clock < due) return;
            // Preserve row separation after stalls rather than releasing overdue steps in a burst.
            start += Math.Max(0, clock - due);
            var step = steps[index];
            connection.PlayGroup(step.tiles, sound, step.intensity, false);
            Status = "Playing step " + (index + 1) + " / " + steps.Length;
            index++;
            if (index < steps.Length) return;
            if (repeat) { index = 0; start += cycle; }
            else { IsRunning = false; Status = "Sequence dispatched; final sound finishes naturally"; }
        }

        public void Stop(FloorServiceConnection connection)
        {
            IsRunning = false; key = null; Status = "Sequence stopped";
            connection.StopAll();
        }
        public void Cancel() { IsRunning = false; key = null; Status = "Sequence cancelled"; }
    }
}
