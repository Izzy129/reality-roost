using System;

namespace RealityRoost.Shared.HapticFloor
{
    public static class HapticFloorEvents
    {
        public static event Action<int, int, float> OnRumbleTriggered;
        public static event Action<int, float> OnIntensityUpdated;
        public static event Action<int> OnRumbleStopped;

        public static void RaiseRumbleTriggered(int tileIndex, int soundIndex, float intensity)
        {
            OnRumbleTriggered?.Invoke(tileIndex, soundIndex, intensity);
        }

        public static void RaiseIntensityUpdated(int tileIndex, float intensity)
        {
            OnIntensityUpdated?.Invoke(tileIndex, intensity);
        }

        public static void RaiseRumbleStopped(int tileIndex)
        {
            OnRumbleStopped?.Invoke(tileIndex);
        }
    }
}
