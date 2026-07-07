using RealityRoost.Client.HapticFloor;
using UnityEngine;

public class HapticFloorTestTrigger : MonoBehaviour
{
    [SerializeField] private HapticFloorClient hapticFloor;

    [Header("Test Values")]
    [SerializeField] private int tileIndex = 0;
    [SerializeField, Range(0f, 1f)] private float intensity = 1f;
    [SerializeField] private int soundIndex = 0;

    [ContextMenu("Test Trigger Rumble")]
    public void TestTriggerRumble()
    {
        if (hapticFloor == null)
        {
            Debug.LogError("[HapticFloorTestTrigger] HapticFloorClient reference is not assigned in Inspector!");
            return;
        }

        hapticFloor.TriggerRumble(tileIndex, soundIndex, intensity);
    }

    [ContextMenu("Test Set Intensity")]
    public void TestSetIntensity()
    {
        if (hapticFloor == null)
        {
            Debug.LogError("[HapticFloorTestTrigger] HapticFloorClient reference is not assigned in Inspector!");
            return;
        }

        hapticFloor.SetIntensity(tileIndex, intensity);
    }

    [ContextMenu("Test Stop Rumble")]
    public void TestStopRumble()
    {
        if (hapticFloor == null)
        {
            Debug.LogError("[HapticFloorTestTrigger] HapticFloorClient reference is not assigned in Inspector!");
            return;
        }

        hapticFloor.StopRumble(tileIndex);
    }
}
