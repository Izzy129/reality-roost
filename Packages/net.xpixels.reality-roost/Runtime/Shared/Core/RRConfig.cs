using System.IO;
using UnityEngine;

namespace RealityRoost.Shared.Core
{
    // Project-wide constants
    public static class RRConfig
    {
        public const string CalibrationFileName = "rr_calibration.json";

        public static string CalibrationFilePath => Path.Combine(Application.persistentDataPath, CalibrationFileName);

        // TODO: add automatic connection details here (host IP, client IPs, etc.)
    }
}
