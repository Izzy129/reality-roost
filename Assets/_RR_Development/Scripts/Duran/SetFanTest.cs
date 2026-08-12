using UnityEngine;
using RealityRoost.Client.Fan;

public class FanTester : MonoBehaviour
{
    public int fanID = 0;
    public float speed = 100f;
    public float pitch = 45f;
    public float yaw = 45f;
    public void TestFan()
    {
        Debug.Log("[FanClient] TestFan called");
        FanClient.Instance.SetFan(fanID, speed, pitch, yaw);
    }

    public void StopFan()
    {
        FanClient.Instance.SetFan(0, 0f, 45f, 45f);
    }
}