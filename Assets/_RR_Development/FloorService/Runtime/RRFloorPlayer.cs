using System;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

namespace RealityRoost.Shared.HapticFloor
{
    /// <summary>
    /// Plays one clip or preset through Floor Service when the scene starts or a script or event triggers it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Reality Roost/Floor Player")]
    public sealed class RRFloorPlayer : MonoBehaviour
    {
        public enum Source { AudioClip, Preset }
        [HideInInspector]
        public RRFloorService floorService;
        public Source source;
        public AudioClip audioClip;
        public FloorSequence preset;
        public FloorTiles tiles = FloorTiles.All;
        [Range(.01f, 1f)] public float intensity = .25f;
        public bool loop;
        [Tooltip("Play once when this component starts, after the host and floor audio are ready. Leave off to trigger Play() from an event or script.")]
        public bool playOnStart;
        [Tooltip("Invoked when this player sends a play request. Audio upload and physical playback may happen later.")]
        public UnityEvent onPlay = new UnityEvent();
        [Tooltip("Invoked when this player sends a stop request to Floor Service.")]
        public UnityEvent onStop = new UnityEvent();
        private bool invokingEvent;
        public bool IsWaitingToStart { get; private set; }
        private bool manuallyTriggered;
        public float PlaybackRate { get; private set; } = 1;

        /// <summary>Adjust preset timing without restarting it. Does not change clip pitch or duration.</summary>
        public void SetPlaybackRate(float rate)
        {
            if (float.IsNaN(rate) || float.IsInfinity(rate)) return;
            PlaybackRate = Mathf.Clamp(rate, 0, 8);
            if (CanPlay && source == Source.Preset) floorService.SetPresetPlaybackRate(this, PlaybackRate);
        }
        public bool CanPlay => Application.isPlaying && isActiveAndEnabled && floorService != null &&
            floorService.isActiveAndEnabled && floorService.CanAccessService;

        private void Reset() { floorService = GetComponent<RRFloorService>(); }
        private void OnEnable()
        {
            if (RRFloorService.Instance != null) floorService = RRFloorService.Instance;
        }

        private void Start()
        {
            if (!playOnStart || manuallyTriggered) return;
            try { Validate(); }
            catch (Exception e) { Debug.LogWarning("[RR] Floor Player: " + e.Message, this); return; }
            IsWaitingToStart = true;
        }

        private void Update()
        {
            if (!IsWaitingToStart) return;
            var manager = NetworkManager.Singleton;
            if (manager != null && (!manager.IsListening || !manager.IsServer)) return;
            if (!CanPlay || !floorService.Connection.IsFresh || !floorService.Connection.Status.audio.ready) return;
            BeginPlay();
        }

        private void OnDisable() { IsWaitingToStart = false; }

        public void Validate()
        {
            if (floorService == null || !floorService.isActiveAndEnabled)
                throw new ArgumentException("Floor Service is unavailable.");
            ValidateEffect();
        }

        public void ValidateEffect()
        {
            if (source == Source.Preset)
            {
                if (preset == null) throw new ArgumentException("Assign a preset.");
                preset.Validate();
            }
            else
            {
                if (audioClip == null) throw new ArgumentException("Assign an Audio Clip.");
                if (tiles == FloorTiles.None || (tiles & ~FloorTiles.All) != 0)
                    throw new ArgumentException("Select at least one valid tile.");
                if (float.IsNaN(intensity) || float.IsInfinity(intensity) || intensity <= 0 || intensity > 1)
                    throw new ArgumentException("Intensity must be greater than zero and at most one.");
            }
        }

        /// <summary>Trigger this effect from an experience event, Timeline reaction, or script on the host.</summary>
        public void BeginPlay()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || invokingEvent) return;
            manuallyTriggered = true;
            IsWaitingToStart = false;
            try { Validate(); }
            catch (Exception e) { Debug.LogWarning("[RR] Floor Player: " + e.Message, this); return; }
            // Shared narrative events may execute on all peers. Only the host drives the hardware.
            if (!CanPlay) return;
            if (source == Source.Preset) floorService.PlayPreset(preset, PlaybackRate, this);
            else floorService.PlayClip(audioClip, "", tiles, intensity, loop);
            InvokeEvent(onPlay);
        }

        /// <summary>Stops all playback on this player's Floor Service, including any preset.</summary>
        public void EndPlay()
        {
            if (invokingEvent) return;
            manuallyTriggered = true;
            IsWaitingToStart = false;
            if (Application.isPlaying && floorService != null && floorService.isActiveAndEnabled && floorService.CanAccessService)
            {
                floorService.StopAll();
                InvokeEvent(onStop);
            }
        }

        // Keep existing scripts and serialized UnityEvent listeners working.
        public void Play() { BeginPlay(); }
        public void Stop() { EndPlay(); }

        private void InvokeEvent(UnityEvent callback)
        {
            // Avoid recursive playback if a listener points back to this player.
            invokingEvent = true;
            try { callback?.Invoke(); }
            finally { invokingEvent = false; }
        }
    }
}
