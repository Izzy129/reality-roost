using RealityRoost.Shared.HapticFloor;
using Unity.Netcode;
using UnityEngine;

namespace RealityRoost.Client.HapticFloor
{
    public class HapticFloorClient : NetworkBehaviour
    {
        // Scene-placed NetworkObject, one per scene
        // RRHapticEmitter and other client code call haptic floor events through this
        public static HapticFloorClient Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[RR][WARN] HapticFloorClient: Another instance already exists in this scene, only one is supported.");
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayClip(int tileIndex, string clipResourcePath, float intensity, bool loop)
        {
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(PlayClip)))
            {
                Debug.LogError("[RR][ERROR] HapticFloorClient: Tile index out of bounds!");
                return;
            }

            if (string.IsNullOrEmpty(clipResourcePath))
            {
                Debug.LogError("[RR][ERROR] HapticFloorClient: clipResourcePath is null or empty.");
                return;
            }

            intensity = HapticFloorUtils.ClampIntensity(intensity, nameof(PlayClip));
            Debug.Log($"[RR][DEBUG] HapticFloorClient: sending haptic request to host: clip {clipResourcePath} on tile {tileIndex} @ {(intensity * 100):0.00} intensity");
            Debug.Log($"[RR][DEBUG] HapticFloorClient: IsSpawned={IsSpawned}, NetworkObjectId={NetworkObjectId}");
            PlayClipServerRpc(tileIndex, clipResourcePath, intensity, loop);
        }

        public void StopRumble(int tileIndex)
        {
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(StopRumble)))
            {
                Debug.LogError("[RR][ERROR] HapticFloorClient: Tile index out of bounds!");
                return;
            }
            Debug.Log($"[RR][DEBUG] HapticFloorClient: sending stop request to host: tile {tileIndex}");

            StopRumbleServerRpc(tileIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PlayClipServerRpc(int tileIndex, string clipResourcePath, float intensity, bool loop)
        {
            Debug.Log("[RR][DEBUG] HapticFloorClient: Client requested clip playback!");
            HapticFloorEvents.RaisePlayClipRequested(tileIndex, clipResourcePath, intensity, loop);
        }

        [ServerRpc(RequireOwnership = false)]
        private void StopRumbleServerRpc(int tileIndex)
        {
            Debug.Log("[RR][DEBUG] HapticFloorClient: Client requested rumble stop!");
            HapticFloorEvents.RaiseRumbleStopped(tileIndex);
        }
        public override void OnNetworkSpawn()
        {
            Debug.Log($"[RR][DEBUG] HapticFloorClient: OnNetworkSpawn — IsServer={IsServer} IsHost={IsHost} IsClient={IsClient} NetworkObjectId={NetworkObjectId} OwnerClientId={OwnerClientId}");
        }
    }
}
