using UnityEngine;
using OpenCVForUnity.UnityIntegration.Helper.AR;

namespace OpenCVForUnityExample
{
    public class ArUcoMarkerTracker : MonoBehaviour
    {

        public ArUcoExample.MarkerType MarkerType = ArUcoExample.MarkerType.CanonicalMarker;
        public ArUcoExample.ArUcoDictionary DictionaryId = ArUcoExample.ArUcoDictionary.DICT_6X6_250;
        public int MarkerId = 0;


        public ArUcoExample ArUcoExampleScript;
        public Camera ARCamera;
        public float DistanceFromCamera = 2.0f;
        public bool UseSmoothing = true;
        [Range(0.01f, 1f)]
        public float SmoothingFactor = 0.3f;
        public bool DebugMode = false;


        public Vector3 PositionOffset = Vector3.zero;
        public Vector3 RotationOffset = Vector3.zero;

        private ARHelper _arHelper;
        private GameObject _trackedMarkerObject;
        private string _targetNamePattern;
        private bool _isTracking = false;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _markerWorldPosition;
        private Quaternion _markerWorldRotation;

        private void Start()
        {
            if (ArUcoExampleScript == null)
            {
                ArUcoExampleScript = Object.FindFirstObjectByType<ArUcoExample>();
                if (ArUcoExampleScript == null)
                {
                    Debug.LogError("ArUcoMarkerTracker: No ArUcoExample script found in scene!");
                    enabled = false;
                    return;
                }
            }

            if (ARCamera == null)
            {
                ARCamera = Camera.main;
                if (ARCamera == null)
                {
                    Debug.LogError("ArUcoMarkerTracker: No ARCamera found! Please assign a camera.");
                    enabled = false;
                    return;
                }
            }

            _arHelper = ArUcoExampleScript.ArHelper;

            if (_arHelper == null)
            {
                Debug.LogError("ArUcoMarkerTracker: ARHelper is null in ArUcoExample!");
                enabled = false;
                return;
            }

            CreateTargetIdentifierString();

            Debug.Log($"ArUcoMarkerTracker initialized. Tracking pattern: {_targetNamePattern}");
        }

        private void CreateTargetIdentifierString()
        {
            string dictionaryName = DictionaryId.ToString();
            string markerTypeName = MarkerType.ToString();

            switch (MarkerType)
            {
                case ArUcoExample.MarkerType.CanonicalMarker:
                    _targetNamePattern = $"{markerTypeName} {dictionaryName} [{MarkerId}]";
                    break;

                case ArUcoExample.MarkerType.GridBoard:
                case ArUcoExample.MarkerType.ChArUcoBoard:
                    _targetNamePattern = $"{markerTypeName} {dictionaryName}";
                    break;

                case ArUcoExample.MarkerType.ChArUcoDiamondMarker:
                    _targetNamePattern = $"{markerTypeName} {dictionaryName}";
                    break;
            }

            if (DebugMode)
                Debug.Log($"Target pattern: {_targetNamePattern}");
        }

        private void Update()
        {
            if (_arHelper == null || _arHelper.ARGameObjects == null || ARCamera == null)
                return;

            FindTrackedMarker();

            if (_isTracking && _trackedMarkerObject != null)
            {
                UpdateTransformFromMarker();
            }
        }

        private void FindTrackedMarker()
        {
            GameObject foundObject = null;

            foreach (ARGameObject arGameObject in _arHelper.ARGameObjects)
            {
                if (arGameObject == null) continue;

                GameObject obj = arGameObject.gameObject;

                if (obj.name.Contains($"[{MarkerId}]") &&
                    obj.name.Contains(DictionaryId.ToString()))
                {
                    foundObject = obj;
                    break;
                }
                else if (MarkerType == ArUcoExample.MarkerType.GridBoard ||
                         MarkerType == ArUcoExample.MarkerType.ChArUcoBoard)
                {
                    if (obj.name.Contains(MarkerType.ToString()) &&
                        obj.name.Contains(DictionaryId.ToString()))
                    {
                        foundObject = obj;
                        break;
                    }
                }
            }

            if (foundObject != null && foundObject != _trackedMarkerObject)
            {
                _trackedMarkerObject = foundObject;

                if (!_isTracking)
                {
                    _isTracking = true;
                    if (DebugMode)
                        Debug.Log($"ArUcoMarkerTracker: Started tracking {_trackedMarkerObject.name}");
                }
            }
            else if (foundObject == null && _isTracking)
            {
                _isTracking = false;
                _trackedMarkerObject = null;
                if (DebugMode)
                    Debug.Log("ArUcoMarkerTracker: Lost tracking of marker");
            }
        }

        private void UpdateTransformFromMarker()
        {
            if (_trackedMarkerObject == null || ARCamera == null)
                return;

            _markerWorldPosition = _trackedMarkerObject.transform.position;
            _markerWorldRotation = _trackedMarkerObject.transform.rotation;

            Vector3 cameraRelativePos = ARCamera.transform.InverseTransformPoint(_markerWorldPosition);

            Vector3 targetCamRelativePos = new Vector3(
                cameraRelativePos.x,
                cameraRelativePos.y,
                DistanceFromCamera
            );

            _targetPosition = ARCamera.transform.TransformPoint(targetCamRelativePos);

            _targetPosition += ARCamera.transform.right * PositionOffset.x;
            _targetPosition += ARCamera.transform.up * PositionOffset.y;
            _targetPosition += ARCamera.transform.forward * PositionOffset.z;

            Vector3 directionToCamera = (ARCamera.transform.position - _targetPosition).normalized;
            _targetRotation = Quaternion.LookRotation(directionToCamera) * Quaternion.Euler(RotationOffset);

            if (UseSmoothing && _isTracking)
            {
                transform.position = Vector3.Lerp(transform.position, _targetPosition, SmoothingFactor);
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, SmoothingFactor);
            }
            else
            {
                transform.position = _targetPosition;
                transform.rotation = _targetRotation;
            }

            if (DebugMode)
            {
                Debug.DrawLine(ARCamera.transform.position, _markerWorldPosition, Color.green);
                Debug.DrawLine(ARCamera.transform.position, transform.position, Color.blue);
            }
        }

        public bool IsTracking()
        {
            return _isTracking && _trackedMarkerObject != null;
        }

        public Vector3 GetMarkerWorldPosition()
        {
            return _markerWorldPosition;
        }

        public Quaternion GetMarkerWorldRotation()
        {
            return _markerWorldRotation;
        }

        public void SetTargetMarker(int markerId)
        {
            MarkerId = markerId;
            CreateTargetIdentifierString();
            _isTracking = false;
            _trackedMarkerObject = null;

            if (DebugMode)
                Debug.Log($"ArUcoMarkerTracker: Target changed to marker ID {markerId}");
        }

        private void OnDisable()
        {
            _isTracking = false;
        }

        private void OnEnable()
        {
            if (_arHelper != null)
            {
                _isTracking = false;
                _trackedMarkerObject = null;
            }
        }
    }
}