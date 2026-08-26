// Attach this to your Raft to simulate floating
using UnityEngine;

public class RaftBobbing : MonoBehaviour
{
    public float rotationSpeed = 1.0f;
    public float tiltAngle = 2.0f; // Keep it low so players don't get dizzy!

    void Update()
    {
        float tilt = Mathf.Sin(Time.time * rotationSpeed) * tiltAngle;
        transform.localRotation = Quaternion.Euler(tilt, 0, tilt * 0.5f);
    }
}