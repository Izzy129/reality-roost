using System;
using UnityEngine;

namespace RealityRoost.Client.SpatialCalibration
{
    // This struct represents a saved piece of Calibration data for Spatial Calibration
    // Serialized to local JSON file with RRCalibrationStore
    // NOT NETWORKED!!! Each client computes its own pose in railing frame (since clients/HMDs have unique XR origins).
    [Serializable]
    public struct CalibrationData
    {
        // Used to track when this struct's layout changes
        // Ensures that older calibration files (which may not be in the expected format) are discarded by RRCalibrationStore
        // Future note: Increment this when this struct's format changes!!
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion;
        public bool IsValid;

        // Capture state in the rig's tracking space
        public Vector3 RawBackLeft;
        public Vector3 RawBackRight;
        public float ManualYawDegrees;

        // Derived applied pose (recomputed from the raw state on load; stored for debugging)
        public Vector3 LocalPosition;
        public float YawDegrees;

        // When this calibration record was written (for logging/auditing)
        public long SavedAtUnixSeconds;

        public Quaternion LocalRotation => Quaternion.Euler(0f, YawDegrees, 0f);

        public static CalibrationData Identity => new CalibrationData
        {
            SchemaVersion = CurrentSchemaVersion,
            IsValid = false,
            RawBackLeft = Vector3.zero,
            RawBackRight = Vector3.zero,
            ManualYawDegrees = 0f,
            LocalPosition = Vector3.zero,
            YawDegrees = 0f,
            SavedAtUnixSeconds = 0L
        };

        // Stamps a solved pose with the raw capture state + save metadata
        public static CalibrationData FromSolve(CalibrationData solved, Vector3 rawBackLeft, Vector3 rawBackRight, float manualYawDegrees)
        {
            solved.RawBackLeft = rawBackLeft;
            solved.RawBackRight = rawBackRight;
            solved.ManualYawDegrees = manualYawDegrees;
            solved.SchemaVersion = CurrentSchemaVersion;
            solved.SavedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return solved;
        }
    }
}
