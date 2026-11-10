using Unity.Netcode;
using UnityEngine;

namespace RealityRoost.Shared.HapticFloor
{
    /// <summary>
    /// Connects the scene to Floor Software on the host and reports when the floor is ready to play.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("Reality Roost/Floor Service")]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class RRFloorService : NetworkBehaviour
    {
        public static RRFloorService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Leave the scene NetworkObject intact for Netcode; only the first service drives hardware.
                enabled = false;
                return;
            }
            Instance = this;
            if (Unity.Netcode.NetworkManager.Singleton == null) DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            // Keep the object in its authored scene until Netcode has registered it.
            if (Instance != this) return;
            NetworkObject.DestroyWithScene = false;
            DontDestroyOnLoad(gameObject);
        }
        [Tooltip("Address of the separately running Floor Software, as seen by the Unity host.")]
        [SerializeField, HideInInspector] private string serviceUrl = "http://127.0.0.1:8765";
        [Tooltip("Drag a Unity AudioClip here. It is uploaded automatically before playback.")]
        [SerializeField] private AudioClip audioClip;
        [SerializeField, HideInInspector] private string clipAssetKey;
        [Tooltip("Used only when no AudioClip is assigned.")]
        [SerializeField] private string soundId = "roost/knock";
        [SerializeField] private FloorTiles selectedTiles = FloorTiles.Tile1;
        [SerializeField, Range(.01f, 1f)] private float intensity = .5f;
        [SerializeField] private bool loop;
        [Header("Narrative sequence")]
        [SerializeField] private FloorSequence sequence;
        [SerializeField, HideInInspector] private string sequenceAssetKey;
        private readonly FloorSequencePlayer sequencePlayer = new FloorSequencePlayer();
        private object presetOwner;
        public FloorSequence Sequence => sequence;
        public string SequenceStatus => sequencePlayer.Status;
        public bool IsSequenceRunning => sequencePlayer.IsRunning;
        public AudioClip Clip => audioClip;
        public FloorTiles SelectedTiles => selectedTiles;
#if UNITY_EDITOR
        private void OnValidate()
        {
            clipAssetKey = audioClip == null ? "" : UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(audioClip));
            sequenceAssetKey = sequence == null ? "" : UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(sequence));
        }
#endif
        public FloorServiceConnection Connection { get; } = new FloorServiceConnection();
        public bool IsReady { get; private set; }
        public event System.Action<bool> ReadyChanged;

        private void SetReady(bool ready)
        {
            if (IsReady == ready) return;
            IsReady = ready;
            ReadyChanged?.Invoke(ready);
        }

        public void PlaySequence()
        {
            if (!isActiveAndEnabled || sequence == null) return;
            if (!CanAccessService)
            {
                if (IsSpawned && IsClient) PlaySequenceServerRpc(sequenceAssetKey);
                else Debug.LogWarning("[RR][WARN] FloorService: Connect to the host before playing a sequence.", this);
                return;
            }
            try
            {
                StartSequence(sequence);
            }
            catch (System.Exception e) { Debug.LogError("[RR][ERROR] FloorService: " + e.Message, this); }
        }
        /// <summary>Play a preset supplied by a runtime Floor Player on the hardware host.</summary>
        public void PlayPreset(FloorSequence preset)
        { PlayPreset(preset, 1, null); }

        public void PlayPreset(FloorSequence preset, float rate, object owner)
        {
            if (!isActiveAndEnabled || preset == null || !CanAccessService) return;
            try { StartSequence(preset, rate, owner); }
            catch (System.Exception e) { Debug.LogError("[RR] Floor Service: " + e.Message, this); }
        }
        public void SetPresetPlaybackRate(object owner, float rate)
        {
            if (owner != null && ReferenceEquals(owner, presetOwner) && sequencePlayer.IsRunning && CanAccessService)
                sequencePlayer.SetPlaybackRate(rate, Time.realtimeSinceStartupAsDouble);
        }

        private void StartSequence(FloorSequence preset, float rate = 1, object owner = null)
        {
            preset.Validate();
            FloorSequencePlayer.ValidateRate(rate);
            var clip = preset.audioClip != null ? preset.audioClip : audioClip;
            var wav = clip == null ? null : FloorClipEncoder.Encode(clip);
            sequencePlayer.Cancel();
            Connection.Tick(serviceUrl);
            Connection.StopAll();
            sequencePlayer.Start(preset, Connection, wav, soundId, rate);
            presetOwner = owner;
        }
        public void StopSequence() { StopAll(); }
        [ServerRpc(RequireOwnership = false)] private void PlaySequenceServerRpc(string key)
        {
            if (sequence != null && !string.IsNullOrEmpty(key) && key == sequenceAssetKey) PlaySequence();
            else Debug.LogError("[RR][ERROR] FloorService: Assign the same sequence preset on the host and client.", this);
        }
        public void ReleaseConnection() { sequencePlayer.Cancel(); Connection.Dispose(); SetReady(false); }
        private void CancelSequenceForManualControl()
        {
            if (CanAccessService && sequencePlayer.IsRunning) sequencePlayer.Stop(Connection);
        }

        public void Play() { PlayConfigured(selectedTiles); }
        public void PlayGroup() { Play(); }
        public void PlayAll() { PlayConfigured(FloorTiles.All); }
        public void Stop() { StopGroup(selectedTiles); }
        private void PlayConfigured(FloorTiles tiles)
        {
            if (audioClip != null) PlayClip(audioClip, clipAssetKey, tiles, intensity, loop);
            else PlayGroup(tiles, soundId, intensity, loop);
        }

        public bool CanAccessService
        {
            get
            {
                if (!Application.isPlaying) return true;
                if (NetworkManager != null && NetworkManager.IsListening) return IsSpawned && IsServer;
#if RR_HOST || UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        private void Update() { TickConnection(); }
        public void TickConnection()
        {
            if (isActiveAndEnabled && CanAccessService)
            {
                Connection.Tick(serviceUrl);
                SetReady(Connection.IsFresh && Connection.Status.audio.ready);
                sequencePlayer.Tick(Connection, Time.realtimeSinceStartupAsDouble);
            }
            else ReleaseConnection();
        }
        private void OnDisable() { ReleaseConnection(); }
        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            ReleaseConnection();
            base.OnDestroy();
        }

        public void Play(int tileIndex, string soundId, float intensity = 1f, bool loop = false)
        {
            if (!isActiveAndEnabled) return;
            CancelSequenceForManualControl();
            if (CanAccessService) { TickConnection(); Connection.Play(tileIndex, soundId, intensity, loop); }
            else if (IsSpawned && IsClient) PlayServerRpc(tileIndex, soundId, intensity, loop);
            else Debug.LogWarning("[RR][WARN] FloorService: Start the network client and spawn the Floor Service NetworkObject before playing.", this);
        }

        public void PlayGroup(FloorTiles tiles, string soundId, float intensity = 1f, bool loop = false)
        {
            if (!isActiveAndEnabled) return;
            CancelSequenceForManualControl();
            if (CanAccessService) { TickConnection(); Connection.PlayGroup(tiles, soundId, intensity, loop); }
            else if (IsSpawned && IsClient) PlayGroupServerRpc((int)tiles, soundId, intensity, loop);
            else Debug.LogWarning("[RR][WARN] FloorService: Start the network client and spawn the Floor Service NetworkObject before playing.", this);
        }
        public void PlayAll(string soundId, float intensity = 1f, bool loop = false)
        { PlayGroup(FloorTiles.All, soundId, intensity, loop); }
        public void PlayClip(AudioClip clip, string assetKey, FloorTiles tiles, float intensity = 1f, bool loop = false)
        {
            if (!isActiveAndEnabled) return;
            CancelSequenceForManualControl();
            if (CanAccessService)
            {
                try { TickConnection(); Connection.PlayWav(tiles, FloorClipEncoder.Encode(clip), intensity, loop); }
                catch (System.Exception e) { Debug.LogError("[RR][ERROR] FloorService: " + e.Message, this); }
            }
            else if (IsSpawned && IsClient && !string.IsNullOrEmpty(assetKey))
                PlayClipServerRpc(assetKey, (int)tiles, intensity, loop);
            else Debug.LogWarning("[RR][WARN] FloorService: Connect to the host and assign the same imported AudioClip to Floor Service in both builds.", this);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlayClipServerRpc(string assetKey, int tiles, float intensity, bool loop)
        {
            // Resolve this controller's shared asset; audio buffers never travel through an RPC.
            if (audioClip != null && !string.IsNullOrEmpty(assetKey) && clipAssetKey == assetKey)
            { PlayClip(audioClip, assetKey, (FloorTiles)tiles, intensity, loop); return; }
            Debug.LogError("[RR][ERROR] FloorService: Assign the same AudioClip to this Floor Service on the host and client.", this);
        }
        public void StopGroup(FloorTiles tiles)
        {
            if (!isActiveAndEnabled) return;
            CancelSequenceForManualControl();
            if (CanAccessService) { TickConnection(); Connection.StopGroup(tiles); }
            else if (IsSpawned && IsClient) StopGroupServerRpc((int)tiles);
        }

        public void Stop(int tileIndex)
        {
            if (!isActiveAndEnabled) return;
            CancelSequenceForManualControl();
            if (CanAccessService) { TickConnection(); Connection.Stop(tileIndex); }
            else if (IsSpawned && IsClient) StopServerRpc(tileIndex);
        }
        public void StopAll()
        {
            if (!isActiveAndEnabled) return;
            sequencePlayer.Cancel();
            if (CanAccessService) { TickConnection(); Connection.StopAll(); }
            else if (IsSpawned && IsClient) StopAllServerRpc();
        }
        [ServerRpc(RequireOwnership = false)] private void PlayServerRpc(int tile, string sound, float intensity, bool loop) { Play(tile, sound, intensity, loop); }
        [ServerRpc(RequireOwnership = false)] private void StopServerRpc(int tile) { Stop(tile); }
        [ServerRpc(RequireOwnership = false)] private void StopAllServerRpc() { StopAll(); }
        [ServerRpc(RequireOwnership = false)] private void PlayGroupServerRpc(int tiles, string sound, float intensity, bool loop) { PlayGroup((FloorTiles)tiles, sound, intensity, loop); }
        [ServerRpc(RequireOwnership = false)] private void StopGroupServerRpc(int tiles) { StopGroup((FloorTiles)tiles); }
    }
}
