using System;
using System.IO;
using RealityRoost.Shared.Core;
using UnityEngine;

namespace RealityRoost.Client.SpatialCalibration
{
    // This script handles storing/loading a calibration from a JSON file on disk.
    // Live calibration data (_current) survives scene switches because it is static.
    // The JSON file makes calibration survive app restarts. 
    // Each client stores its own calibration file in RRConfig.CalibrationFilePath
    
    public static class RRCalibrationStore
    {
        // In-memory calibration data
        private static CalibrationData _current = CalibrationData.Identity;

        // On-disk calibration data file path
        private static string CalibrationFilePath = RRConfig.CalibrationFilePath;
        // Indicates whether calibration data was successfully loaded from disk
        private static bool _loaded;
        private static bool _loggedPath;

        // Lazily loaded from disk on first access. Reload happens once per process
        public static CalibrationData Current
        {
            get
            {
                EnsureLoaded();
                return _current;
            }
        }

        // Update the live value without updating file (used by runtime resets)
        public static void SetCurrent(CalibrationData data)
        {
            _current = data;
            _loaded = true;
        }

        // Update the live value and persist it
        public static void Save(CalibrationData data)
        {
            SetCurrent(data);
            WriteToDisk(data);
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }
            _loaded = true;
            _current = ReadFromDisk();
        }

        private static CalibrationData ReadFromDisk()
        {
            if (!File.Exists(CalibrationFilePath))
            {
                LogWarn($"Calibration file does not exist at {CalibrationFilePath}. Starting uncalibrated.");
                return CalibrationData.Identity;
            }

            try
            {
                string json = File.ReadAllText(CalibrationFilePath);
                CalibrationData data = JsonUtility.FromJson<CalibrationData>(json);
                if (data.SchemaVersion != CalibrationData.CurrentSchemaVersion)
                {
                    LogWarn($"Calibration file (at {CalibrationFilePath}) schema {data.SchemaVersion} does not match expected schema {CalibrationData.CurrentSchemaVersion}." + 
                            "File was written using an older calibration data format." +
                            "Starting uncalibrated.");
                    return CalibrationData.Identity;
                }
                LogInfo($"Loaded calibration from {CalibrationFilePath} (saved {data.SavedAtUnixSeconds} unix).");
                return data;
            }
            catch (Exception e)
            {
                LogWarn($"Failed to read calibration file at {CalibrationFilePath} ({e.Message}). Starting uncalibrated.");
                return CalibrationData.Identity;
            }
        }


        private static void WriteToDisk(CalibrationData data)
        {
            try
            {
                string directory = Path.GetDirectoryName(CalibrationFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(CalibrationFilePath, json);
                if (!_loggedPath)
                {
                    _loggedPath = true;
                    LogInfo($"Calibration saved to {CalibrationFilePath}.");
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to write calibration file at {CalibrationFilePath} ({e.Message}).");
            }
        }

        private static void LogInfo(string message) => Debug.Log($"[RR][INFO] CalibStore: {message}");
        private static void LogWarn(string message) => Debug.LogWarning($"[RR][WARN] CalibStore: {message}");
        private static void LogError(string message) => Debug.LogError($"[RR][ERROR] CalibStore: {message}");
    }
}
