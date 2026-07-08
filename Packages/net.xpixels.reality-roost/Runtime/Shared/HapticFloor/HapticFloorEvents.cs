using System;

namespace RealityRoost.Shared.HapticFloor
{
    public static class HapticFloorEvents
    {
        // tileIndex, clipResourcePath (Resources-relative, no extension), intensity, loop
        public static event Action<int, string, float, bool> OnPlayClipRequested;
        public static event Action<int> OnRumbleStopped;

        public static void RaisePlayClipRequested(int tileIndex, string clipResourcePath, float intensity, bool loop)
        {
            OnPlayClipRequested?.Invoke(tileIndex, clipResourcePath, intensity, loop);
        }

        public static void RaiseRumbleStopped(int tileIndex)
        {
            OnRumbleStopped?.Invoke(tileIndex);
        }
    }
}
