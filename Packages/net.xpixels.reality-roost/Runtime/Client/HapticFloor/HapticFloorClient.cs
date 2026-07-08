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
                return;
            }

            if (string.IsNullOrEmpty(clipResourcePath))
            {
                Debug.LogError("[RR][ERROR] HapticFloorClient: clipResourcePath is null or empty.");
                return;
            }

            intensity = HapticFloorUtils.ClampIntensity(intensity, nameof(PlayClip));

            PlayClipServerRpc(tileIndex, clipResourcePath, intensity, loop);
        }

        public void StopRumble(int tileIndex)
        {
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(StopRumble)))
            {
                return;
            }

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
    }
}
