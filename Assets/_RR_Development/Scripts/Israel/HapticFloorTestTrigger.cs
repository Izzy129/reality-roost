using RealityRoost.Client.HapticFloor;
using UnityEngine;

public class HapticFloorTestTrigger : MonoBehaviour
{
    [Header("Test Values")]
    [SerializeField] private int tileIndex = 0;
    [SerializeField] private string clipResourcePath = "RumbleSounds/Footstep";
    [SerializeField, Range(0f, 1f)] private float intensity = 1f;
    [SerializeField] private bool loop = false;

    [ContextMenu("Test Play Clip")]
    public void TestPlayClip()
    {
        if (HapticFloorClient.Instance == null)
        {
            Debug.LogError("[HapticFloorTestTrigger] No HapticFloorClient.Instance in the scene!");
            return;
        }

        HapticFloorClient.Instance.PlayClip(tileIndex, clipResourcePath, intensity, loop);
    }

    [ContextMenu("Test Stop Rumble")]
    public void TestStopRumble()
    {
        if (HapticFloorClient.Instance == null)
        {
            Debug.LogError("[HapticFloorTestTrigger] No HapticFloorClient.Instance in the scene!");
            return;
        }

        HapticFloorClient.Instance.StopRumble(tileIndex);
    }
}
