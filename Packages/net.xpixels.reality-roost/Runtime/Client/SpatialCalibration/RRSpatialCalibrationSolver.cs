using UnityEngine;

namespace RealityRoost.Client.SpatialCalibration
{
    // pure yaw-only two-point rigid fit based on Kabsch algorithm (https://en.wikipedia.org/wiki/Kabsch_algorithm)
    // 2 point, 1 DoF special case of Kabsch algo (closed form, no SVD needed!!)

    // maps two physical captured railing corners (captured from controllers) onto virtual railing's back corners.
    // this mapping produces the client's XR Origin's pose in railing/room space
    // Y is ignored (floor height comes from OpenXR's Floor tracking mode), so only Euler-Y rotation and XZ translation are solved

    // Returns
    public static class RRSpatialCalibrationSolver
    {
        // rawBackLeft, rawBackRight = captured corner points in XR Origin (tracking) space (from controllers)
        // width = railing X span between the two back corners (meters) (physical railing dimensions)
        // depth = railing Z span front-to-back (meters) (physical railing dimensions)

        // Output: the rigid transform (rotation [about Y] + translation [in XZ plane]) that maps a client's XR HMD tracking space into railing/room space.
        public static CalibrationData Solve(Vector3 rawBackLeft, Vector3 rawBackRight, float width, float depth)
        {   // components for center point vector (vector from back-left corner to back-right corner)  
            float dx = rawBackRight.x - rawBackLeft.x;
            float dz = rawBackRight.z - rawBackLeft.z;

            
            // yaw (about y-axis) that rotates above captured center point vector to the railing's +X axis 
            // 2 point, 1 DoF special case of Kabsch algo (no least squares or SVD needed yay!!)
            // we don't need to rotate about X or Z axis thanks to OpenXR's gravity axis floor plane matching thing (thanks to HMD's IMU/SLAM stuff)
            float yawDeg = Mathf.Atan2(dz, dx) * Mathf.Rad2Deg; // amount to rotate
            Quaternion r = Quaternion.Euler(0f, yawDeg, 0f); // rotate about y such that center point vector aligns with +X axis
            // above is basically least-squares/SVD portion of Kabsch's algo, collapsed to closed form


            // translate captured midpoint (between rawBackLeft and rawBackRight) in x and z direction 
            // such that the midpoint aligns with the expected midpoint
            // basically centroid matching step of Kabsch's algo, but collapsed to midpoint (2 points' centroid is just midpoint)
            Vector3 rawMid = (rawBackLeft + rawBackRight) * 0.5f; // midpoint of captured points
            Vector3 expectedMid = new Vector3(0f, 0f, -depth * 0.5f); // expected midpoint using physical railing dimensions
            
            Vector3 finalPos = expectedMid - (r * rawMid); // final position after rotation and translation
            finalPos.y = 0f;

            return new CalibrationData
            {
                LocalPosition = finalPos,
                YawDegrees = yawDeg,
                IsValid = true
            };
        }
    }
}
