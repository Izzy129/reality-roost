using extOSC;
using RealityRoost.Host.Core;
using RealityRoost.Shared.HapticFloor;
using UnityEngine;

namespace RealityRoost.Host.HapticFloor
{
    public class HapticDriver : MonoBehaviour
    {
        [SerializeField] private OscMessenger oscMessenger;

        // debug cache of the last known state sent to Python
        private readonly TileState[] tileStates = new TileState[HapticConstants.TILE_COUNT];

        private struct TileState
        {
            public int SoundIndex;
            public float Intensity;
        }

        private void OnEnable()
        {
            HapticFloorEvents.OnRumbleTriggered += HandleRumbleTriggered;
            HapticFloorEvents.OnIntensityUpdated += HandleIntensityUpdated;
            HapticFloorEvents.OnRumbleStopped += HandleRumbleStopped;
        }

        private void OnDisable()
        {
            HapticFloorEvents.OnRumbleTriggered -= HandleRumbleTriggered;
            HapticFloorEvents.OnIntensityUpdated -= HandleIntensityUpdated;
            HapticFloorEvents.OnRumbleStopped -= HandleRumbleStopped;
        }

        private void HandleRumbleTriggered(int tileIndex, int soundIndex, float intensity)
        {
            Debug.Log("[RR][DEBUG] HapticDriver: Host received rumble trigger from client");
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(HandleRumbleTriggered)))
            {
                return;
            }

            intensity = HapticFloorUtils.ClampIntensity(intensity, nameof(HandleRumbleTriggered));
            tileStates[tileIndex] = new TileState { SoundIndex = soundIndex, Intensity = intensity };

            var message = new OSCMessage("/tile/trigger");
            message.AddValue(OSCValue.Int(tileIndex));
            message.AddValue(OSCValue.Int(soundIndex));
            message.AddValue(OSCValue.Float(intensity));

            oscMessenger.Send(message);
        }

        private void HandleIntensityUpdated(int tileIndex, float intensity)
        {
            Debug.Log("[RR][DEBUG] HapticDriver: Host received intensity update from client!");
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(HandleIntensityUpdated)))
            {
                return;
            }

            intensity = HapticFloorUtils.ClampIntensity(intensity, nameof(HandleIntensityUpdated));
            var state = tileStates[tileIndex];
            state.Intensity = intensity;
            tileStates[tileIndex] = state;

            var message = new OSCMessage("/tile/intensity");
            message.AddValue(OSCValue.Int(tileIndex));
            message.AddValue(OSCValue.Float(intensity));

            oscMessenger.Send(message);
        }

        private void HandleRumbleStopped(int tileIndex)
        {
            Debug.Log("[RR][DEBUG] HapticDriver: Host received rumble stop from client!");
            if (!HapticFloorUtils.IsValidTileIndex(tileIndex, nameof(HandleRumbleStopped)))
            {
                return;
            }

            tileStates[tileIndex] = default;

            var message = new OSCMessage("/tile/stop");
            message.AddValue(OSCValue.Int(tileIndex));

            oscMessenger.Send(message);
        }
    }
}
