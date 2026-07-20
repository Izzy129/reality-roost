using System;
using RealityRoost.Shared.Core;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealityRoost.Client.SpatialCalibration
{
    // Operator-run, setup-time two-point spatial calibration.
    // Aligns this XR Origin's tracking space to the physical safety railing
    
    // Sits on the XR Origin rig root and moves this.transform (the XR Origin) under railingAnchor
    [RequireComponent(typeof(XROrigin))]
    public class RRSpatialCalibrator : RRSubsystem
    {
        protected override string SubsystemName => "SpatialCalib";

        private enum State { Idle, AwaitBackLeft, AwaitBackRight, Calibrated }

        // Which captured corner the panel's X/Z nudges move (L Rail / R Rail selectors).
        public enum RailCorner { BackLeft, BackRight }

        [Header("References")]
        [Tooltip("Railing frame the rig is parented under (canonical pose). Defaults to this rig's parent.")]
        [SerializeField] private Transform railingAnchor;
        [Tooltip("Controller tip used to touch each physical corner.")]
        [SerializeField] private Transform captureTip;
        [Tooltip("Button (e.g. right trigger / activate) that captures the next corner.")]
        [SerializeField] private InputActionReference captureAction;

        [Header("Railing dimensions (meters)")]
        [SerializeField] private float railingWidth = 1.9304f; // X span between the two back corners
        [SerializeField] private float railingDepth = 2.8448f; // Z span front-to-back

        [Header("Debug")]
        [Tooltip("Sphere transforms (children of this rig) to visualize the current back-left/back-right corners.")]
        [SerializeField] private Transform backLeftCornerMarker;
        [SerializeField] private Transform backRightCornerMarker;

        public bool IsCalibrated { get; private set; }
        public event Action OnCalibrated;

        // Fired after a persisted calibration is re-applied on scene/app load
        public event Action<CalibrationData> OnCalibrationLoaded;
        // Fired after the current calibration is written to the store (solve + each nudge)
        public event Action OnCalibrationSaved;
        // Fired when the operator resets calibration to identity
        public event Action OnCalibrationReset;
        // Fired when the nudge target switches between the L Rail / R Rail corners (solve + reset + select).
        public event Action<RailCorner> OnSelectedCornerChanged;

        // Which captured corner the panel's X/Z nudges currently move. For RRCalibrationPanel
        public RailCorner SelectedCorner => _selectedCorner;

        // Current railing-local calibration (updated by solve + nudges). For RRCalibrationPanel
        public CalibrationData Current => _current;

        private XROrigin _xrOrigin;
        private State _state = State.Idle;
        private Vector3 _rawBackLeft;
        private Vector3 _rawBackRight;
        private RailCorner _selectedCorner = RailCorner.BackLeft;
        private float _manualYawDeg;  // extra yaw from the panel's rot buttons, applied on top of the solve
        private CalibrationData _current = CalibrationData.Identity;

        protected override void OnSubsystemAwake()
        {
            _xrOrigin = GetComponent<XROrigin>();
            if (railingAnchor == null)
            {
                railingAnchor = transform.parent;
            }
            SeedCanonicalCorners();
        }

        // Start the raw corners at the virtual railing's own back corners
        private void SeedCanonicalCorners()
        {
            _rawBackLeft = new Vector3(-railingWidth * 0.5f, 0f, -railingDepth * 0.5f);
            _rawBackRight = new Vector3(railingWidth * 0.5f, 0f, -railingDepth * 0.5f);
            _manualYawDeg = 0f;
            UpdateCornerMarkers();
        }

        private void UpdateCornerMarkers()
        {
            if (backLeftCornerMarker != null)
            {
                backLeftCornerMarker.localPosition = _rawBackLeft;
            }
            if (backRightCornerMarker != null)
            {
                backRightCornerMarker.localPosition = _rawBackRight;
            }
        }

        protected override void OnSubsystemStart()
        {
            // Colocation + full-body trackers require floor/stage tracking (y=0 at the physical floor).
            // RRSpatialCalibrationSolver also assumes fixed floor height 
            if (_xrOrigin != null)
            {
                _xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            }

            if (captureAction != null && captureAction.action != null)
            {
                captureAction.action.performed += OnCapturePerformed;
                captureAction.action.Enable();
            }

            // Re-apply a persisted calibration so this scene's fresh rig starts aligned instead of at identity
            CalibrationData stored = RRCalibrationStore.Current;
            if (stored.IsValid)
            {
                ApplyStored(stored);
            }
        }

        protected override void OnSubsystemStop()
        {
            if (captureAction != null && captureAction.action != null)
            {
                captureAction.action.performed -= OnCapturePerformed;
            }
        }

        private void OnCapturePerformed(InputAction.CallbackContext _) => CaptureNextCorner();

        // ---- Public API (menu / UI hooks) ----

        public void BeginCalibration()
        {
            _state = State.AwaitBackLeft;
            _manualYawDeg = 0f;
            LogInfo("Calibration started! Touch the outer BACK-LEFT railing corner, then capture with right thumbstick click.");
        }

        public void CaptureNextCorner()
        {
            if (captureTip == null)
            {
                LogError("captureTip is not assigned! Cannot capture corner.");
                return;
            }

            if (_state == State.Idle || _state == State.Calibrated)
            {
                BeginCalibration();
            }

            if (_state == State.AwaitBackLeft)
            {
                _rawBackLeft = CaptureTipInTrackingSpace();
                _state = State.AwaitBackRight;
                UpdateCornerMarkers();
                LogInfo("Back-left captured. Now touch the outer BACK-RIGHT railing corner, then capture with right thumbstick click.");
            }
            else if (_state == State.AwaitBackRight)
            {
                _rawBackRight = CaptureTipInTrackingSpace();
                UpdateCornerMarkers();
                SolveAndApply();
            }
        }

        public void ResetCalibration()
        {
            SeedCanonicalCorners();
            _selectedCorner = RailCorner.BackLeft;
            OnSelectedCornerChanged?.Invoke(_selectedCorner);
            _current = CalibrationData.Identity;
            IsCalibrated = false;
            _state = State.Idle;
            ApplyCurrent();
            // In-memory only. Saved file is left untouched until the next save overwrites it.
            RRCalibrationStore.SetCurrent(CalibrationData.Identity);

            OnCalibrationReset?.Invoke();
            LogInfo("Calibration reset to identity (in memory; saved file untouched).");
        }

        // Restore a persisted calibration: reconstruct the raw capture state, re-solve, and apply
        private void ApplyStored(CalibrationData record)
        {
            _rawBackLeft = record.RawBackLeft;
            _rawBackRight = record.RawBackRight;
            _manualYawDeg = record.ManualYawDegrees;
            UpdateCornerMarkers();
            Recompute(); // solve + apply, no save
            _state = State.Calibrated;
            OnSelectedCornerChanged?.Invoke(_selectedCorner);
            LogInfo($"Applied stored calibration. yaw {_current.YawDegrees:0.##}°, local pos {_current.LocalPosition}.");
            OnCalibrationLoaded?.Invoke(_current);
        }

        // Stamp the raw capture state + metadata onto the current pose and persist it.
        private void PersistCurrent()
        {
            _current = CalibrationData.FromSolve(_current, _rawBackLeft, _rawBackRight, _manualYawDeg);
            RRCalibrationStore.Save(_current);
            OnCalibrationSaved?.Invoke();
        }

        private void OnApplicationQuit()
        {
            if (IsCalibrated)
            {
                PersistCurrent();
            }
        }

        // ---- Per-corner nudging (panel: L Rail / R Rail pick the target, X/Z move it) ----

        // Choose which captured corner the X/Z nudges move.
        public void SelectBackLeft()
        {
            _selectedCorner = RailCorner.BackLeft;
            OnSelectedCornerChanged?.Invoke(_selectedCorner);
            LogInfo("Nudge target: BACK-LEFT rail point (L Rail).");
        }

        public void SelectBackRight()
        {
            _selectedCorner = RailCorner.BackRight;
            OnSelectedCornerChanged?.Invoke(_selectedCorner);
            LogInfo("Nudge target: BACK-RIGHT rail point (R Rail).");
        }

        // Move the selected corner's captured point, in the rig's tracking space, then re-solve.
        public void NudgeSelectedX(float delta) => NudgeSelectedCorner(new Vector3(delta, 0f, 0f));
        public void NudgeSelectedZ(float delta) => NudgeSelectedCorner(new Vector3(0f, 0f, delta));

        private void NudgeSelectedCorner(Vector3 trackingDelta)
        {
            if (_selectedCorner == RailCorner.BackLeft)
            {
                _rawBackLeft += trackingDelta;
            }
            else
            {
                _rawBackRight += trackingDelta;
            }
            UpdateCornerMarkers();
            Recompute();
            PersistCurrent();
            LogDebug($"Nudged {_selectedCorner} by {trackingDelta} — yaw {_current.YawDegrees:0.##}°, pos {_current.LocalPosition}.");
        }

        // Manual yaw trim (panel rot buttons), applied on top of the two-point solve
        public void NudgeYaw(float deltaDegrees)
        {
            _manualYawDeg += deltaDegrees;
            Recompute();
            PersistCurrent();
            LogDebug($"Manual yaw offset {_manualYawDeg:0.##}° — total yaw {_current.YawDegrees:0.##}°.");
        }

        // Re-express the captured raw corners across a tracking-origin change (e.g. HMD recenter)
        // and re-solve. Called by RRTrackingOriginCompensator with the old->new tracking-space
        // delta transform it derives from the head pose jump across the event.
        public void CompensateRawCorners(Quaternion deltaRotation, Vector3 deltaTranslation)
        {
            _rawBackLeft = deltaRotation * _rawBackLeft + deltaTranslation;
            _rawBackRight = deltaRotation * _rawBackRight + deltaTranslation;
            UpdateCornerMarkers();
            Recompute();
        }

        // ---- Internals ----

        private Vector3 CaptureTipInTrackingSpace()
        {
            // Convert the tip's world pose into XR Origin space => raw tracking-space point,
            // invariant to whatever calibration is currently applied to the rig.
            return transform.InverseTransformPoint(captureTip.position);
        }

        private void SolveAndApply()
        {
            Recompute();
            _state = State.Calibrated;
            PersistCurrent();
            LogInfo($"Calibrated! yaw {_current.YawDegrees:0.##}°, local pos {_current.LocalPosition}. Use the panel to fine-tune.");
            OnCalibrated?.Invoke();
        }

        // Solve the rig pose from the (possibly nudged) raw corners, then apply the manual yaw trim.
        private void Recompute()
        {
            CalibrationData solved = RRSpatialCalibrationSolver.Solve(_rawBackLeft, _rawBackRight, railingWidth, railingDepth);
            if (Mathf.Abs(_manualYawDeg) > Mathf.Epsilon)
            {
                // Rotate about the railing center (railingAnchor origin), not the rig's own origin.
                Quaternion dR = Quaternion.Euler(0f, _manualYawDeg, 0f);
                solved.LocalPosition = dR * solved.LocalPosition;
                solved.YawDegrees += _manualYawDeg;
            }
            _current = solved;
            IsCalibrated = true;
            ApplyCurrent();
        }

        private void ApplyCurrent()
        {
            if (railingAnchor != null && transform.parent != railingAnchor)
            {
                transform.SetParent(railingAnchor, worldPositionStays: false);
            }
            transform.localRotation = _current.LocalRotation;
            transform.localPosition = _current.LocalPosition;
        }
    }
}
