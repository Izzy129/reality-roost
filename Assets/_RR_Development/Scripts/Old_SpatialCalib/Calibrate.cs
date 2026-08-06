// using UnityEngine;
// using UnityEngine.InputSystem;

// public class Calibrate : MonoBehaviour
// {
//     [Header("References")]
//     public Transform calibrationOffset;  // CalibrationOffset parent of XROrigin

//     [Header("Settings")]
//     public Key calibrationKey = Key.C;
//     public bool isCalibratedFlag = false;

//     void Update()
//     {
//         if (Keyboard.current != null && Keyboard.current[calibrationKey].wasPressedThisFrame)
//             CalibrateToMarker();
//     }

//     public void CalibrateToMarker()
//     {
//         // reset to identity first so readings are in raw tracking space
//         // SetPositionAndRotation -> useful Transform method
//         calibrationOffset.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

//         // correct yaw so headset faces room +Z
//         float headsetYaw = Camera.main.transform.eulerAngles.y;
//         calibrationOffset.rotation = Quaternion.Euler(0f, -headsetYaw, 0f);

//         // correct horizontal translation so headset is at room (0,_,0)
//         // we dont calibrate y since ppl have different heights
//         Vector3 camPos = Camera.main.transform.position;
//         calibrationOffset.position = new Vector3(-camPos.x, 0f, -camPos.z);

//         isCalibratedFlag = true;
//         Debug.Log($"[Colocation] Calibration done. Offset pos={calibrationOffset.position}, yaw={-headsetYaw}");
//     }
// }
