using OpenCVForUnityExample;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

public class TwoMarkerCalibrate : MonoBehaviour
{

    public AruCoMarkerTracker headsetMarker;

    //get the real camera pos prior to running this 
    public Vector3 realCameraPos;

    public GameObject playerRig;
    public GameObject playerCamera;
    public Quaternion rotationOffset;

    public float timeBetweenCalibrationAttempts;

    public float rotScalar;
    private void Start()
    {
        StartCoroutine(CalibrationCycleCoroutine()); 
    }

    IEnumerator CalibrationCycleCoroutine()
    {
        while (true)
        {
            CalibrateWithTwoMarkers();
            yield return new WaitForSeconds(timeBetweenCalibrationAttempts);
        }

    }

    void CalibrateWithTwoMarkers()
    {
        Vector3 headsetPos = headsetMarker.MarkerPosition;
        Quaternion headsetRot = headsetMarker.MarkerRotation;
        UnityEngine.Debug.Log("calibrated. player position: " + headsetPos);
        playerRig.transform.position = headsetPos;
        playerCamera.transform.rotation = new Quaternion(headsetRot.x * rotScalar + rotationOffset.x, headsetRot.y * rotScalar + rotationOffset.y, headsetRot.z * rotScalar + rotationOffset.z, 0);

    }
}
