using System;
using UnityEngine;

namespace RealityRoost.Shared.Fan
{
    public static class FanEvents
    {
        // tileIndex, clipResourcePath (Resources-relative, no extension), intensity, loop
        public static event Action<int, float, float, float> OnSetFanRequested;
        public static event Action<int[], float, float, float> OnSetFanGroupRequested;
        //public static event Action</*to be decided*/> OnPointFanAt;

        public static void RaiseSetFanRequested(int nodeIndex, float fanSpeed, float pitch, float yaw)
        {
            Debug.Log("[FanEvents] RaiseSetFanRequested called");
            OnSetFanRequested?.Invoke(nodeIndex, fanSpeed, pitch, yaw);

        }

        public static void RaiseSetFanGroupRequested(int[] nodeIndices, float fanSpeed, float pitch, float yaw)
        {
            OnSetFanGroupRequested?.Invoke(nodeIndices, fanSpeed, pitch, yaw);
        }

        // public static void RaisePointFanAt(/*to be decided*/)
        // {
        //     OnPointFanAt?.Invoke(/*to be decided*/);
        // }
    }
}