using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.UnityIntegration.Helper.AR;

namespace OpenCVForUnityExample
{
    public class AruCoMarkerTracker : MonoBehaviour
    {
        public ARHelper ArHelper;
        public int MarkerId;
        public string MarkerDictionary;
        public GameObject ObjectToPlace;
        public Vector3 PositionOffset = Vector3.zero;
        public Vector3 RotationOffset = Vector3.zero;
        public bool UseSmoothing = true;
        [Range(0.01f, 1f)]
        public float SmoothingFactor = 0.3f;
        public bool DebugMode = false;
        public Vector3 MarkerPosition;
        public Quaternion MarkerRotation;
        private GameObject _trackedMarkerObject;
        private bool _isTracking = false;

        private void Update()
        {
            if (ArHelper == null || ArHelper.ARGameObjects == null || ObjectToPlace == null) return;
            FindTrackedMarker();
            if (_isTracking) UpdateObjectPosition();
        }

        private void FindTrackedMarker()
        {
            _trackedMarkerObject = null;
            _isTracking = false;

            foreach (var arObj in ArHelper.ARGameObjects)
            {
                if (arObj == null || arObj.gameObject == null) continue;
                var go = arObj.gameObject;
                if (go.name.Contains($"[{MarkerId}]") && go.name.Contains(MarkerDictionary))
                {
                    _trackedMarkerObject = go;
                    _isTracking = true;
                    break;
                }
            }
        }

        private void UpdateObjectPosition()
        {
            if (_trackedMarkerObject == null) return;
            MarkerPosition = _trackedMarkerObject.transform.position;
            MarkerRotation = _trackedMarkerObject.transform.rotation;

            Vector3 targetWorldPos = MarkerPosition + MarkerPositionOffset();
            Quaternion targetRotation = MarkerRotation * Quaternion.Euler(RotationOffset);

            Transform parent = ObjectToPlace.transform.parent != null ? ObjectToPlace.transform.parent : ObjectToPlace.transform;

            if (UseSmoothing)
            {
                ObjectToPlace.transform.localPosition = Vector3.Lerp(ObjectToPlace.transform.localPosition, parent.InverseTransformPoint(targetWorldPos), SmoothingFactor);
                ObjectToPlace.transform.localRotation = Quaternion.Slerp(ObjectToPlace.transform.localRotation, targetRotation, SmoothingFactor);
            }
            else
            {
                ObjectToPlace.transform.localPosition = parent.InverseTransformPoint(targetWorldPos);
                ObjectToPlace.transform.localRotation = targetRotation;
            }

            if (DebugMode)
            {
                Debug.DrawLine(Camera.main.transform.position, MarkerPosition, Color.green);
                Debug.DrawLine(Camera.main.transform.position, ObjectToPlace.transform.position, Color.blue);
            }
        }

        private Vector3 MarkerPositionOffset()
        {
            return ObjectToPlace.transform.right * PositionOffset.x +
                   ObjectToPlace.transform.up * PositionOffset.y +
                   ObjectToPlace.transform.forward * PositionOffset.z;
        }
    }
}