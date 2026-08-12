using UnityEngine;

namespace RealityRoost.Shared.Fan
{
    public static class FanUtils
    {
        //Confirm that fanIndex is a valid fan index.
        public static bool IsValidFanIndex(int fanIndex, string caller)
        {
            if (fanIndex < 0 || fanIndex >= FanConstants.FAN_COUNT)
            {
                Debug.LogError($"[RR][ERROR] {caller}: fanIndex {fanIndex} out of range [0, {FanConstants.FAN_COUNT}).");
                return false;
            }

            return true;
        }

        //Confirm that the inputted fan speed is not out of range.
        public static float ClampSpeed(float fanSpeed, string caller)
        {
            if (fanSpeed < 0f || fanSpeed > 100f)
            {
                Debug.LogWarning($"[RR][WARN] {caller}: fanSpeed {fanSpeed} out of range [0.0, 1.0]. Clamping and proceeding...");
            }

            return Mathf.Clamp(fanSpeed, 0, 100);
        }
        //Confirm that the inputted pitch or yaw is not out of range.
        public static float ClampAngle(float angle, string caller)
        {
            if (angle < 0f || angle > 90f)
            {
                Debug.LogWarning($"[RR][WARN] {caller}: angle {angle} out of range [0.0, 90.0]. Clamping and proceeding...");
            }

            return Mathf.Clamp(angle, 0f, 90f);
        }

    }
}