using System.IO;
using UnityEngine;

namespace RealityRoost.Shared.Core
{
    // Project-wide constants
    public static class RRConfig
    {
        public const string CalibrationFileName = "rr_calibration.json";

        public static string CalibrationFilePath => Path.Combine(Application.persistentDataPath, CalibrationFileName);

        public const string NetworkFileName = "rr_network.json";

        public static string NetworkFilePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", NetworkFileName));
    }
}
