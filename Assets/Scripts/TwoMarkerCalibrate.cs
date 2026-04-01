using OpenCVForUnityExample;
using UnityEngine;

public class TwoMarkerCalibrate : MonoBehaviour
{
    public AruCoMarkerTracker headsetMarker;

    public GameObject playerRig;
    public GameObject playerCamera;
    public Quaternion rotationOffset;

    public bool calibrateOnlyOnSpace = false;
    public float smoothSpeed = 5f;

    private void Update()
    {
        if (!calibrateOnlyOnSpace || Input.GetKey(KeyCode.Space))
        {
            CalibrateSmooth();
        }
    }

    void CalibrateSmooth()
    {
        Vector3 targetPos = headsetMarker.MarkerPosition;
        Quaternion targetRot = headsetMarker.MarkerRotation;

        targetPos = new Vector3(-targetPos.x, -targetPos.y, targetPos.z);
        Quaternion correctedRot = new Quaternion(-targetRot.x, -targetRot.y, targetRot.z, targetRot.w) * rotationOffset;

        playerRig.transform.position = Vector3.Lerp(playerRig.transform.position, targetPos, Time.deltaTime * smoothSpeed);
        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, correctedRot, Time.deltaTime * smoothSpeed);
    }
}