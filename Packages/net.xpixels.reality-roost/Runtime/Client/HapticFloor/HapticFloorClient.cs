using RealityRoost.Shared.HapticFloor;
using Unity.Netcode;
using UnityEngine;

namespace RealityRoost.Client.HapticFloor
{
    public class HapticFloorClient : NetworkBehaviour
    {
        public void TriggerRumble(int tileIndex, int soundIndex, float intensity)
        {
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(TriggerRumble)))
            {
                return;
            }

            intensity = HapticFloorUtils.ClampIntensity(intensity, nameof(TriggerRumble));

            TriggerRumbleServerRpc(tileIndex, soundIndex, intensity);
        }

        public void SetIntensity(int tileIndex, float intensity)
        {
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(SetIntensity)))
            {
                return;
            }

            intensity = HapticFloorUtils.ClampIntensity(intensity, nameof(SetIntensity));

            SetIntensityServerRpc(tileIndex, intensity);
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
        private void TriggerRumbleServerRpc(int tileIndex, int soundIndex, float intensity)
        {
            Debug.Log("[RR][DEBUG] HapticFloorClient: Client requested rumble trigger!");
            HapticFloorEvents.RaiseRumbleTriggered(tileIndex, soundIndex, intensity);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetIntensityServerRpc(int tileIndex, float intensity)
        {
            Debug.Log("[RR][DEBUG] HapticFloorClient: Client requested intensity update!");
            HapticFloorEvents.RaiseIntensityUpdated(tileIndex, intensity);
        }

        [ServerRpc(RequireOwnership = false)]
        private void StopRumbleServerRpc(int tileIndex)
        {
            Debug.Log("[RR][DEBUG] HapticFloorClient: Client requested rumble stop!");
            HapticFloorEvents.RaiseRumbleStopped(tileIndex);
        }
    }
}
