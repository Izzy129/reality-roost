using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace RealityRoost.Editor
{
    public static class RRBuildProfileImporter
    {
#if UNITY_6000_0
        // Add scenes to build profile
        [MenuItem("Reality Roost/Add Build Profile", priority = 99)]
        public static void AddScenesToBuildProfile()
        {
            var source = "Packages/net.xpixels.reality-roost/Editor/Build Profiles/RR-Host.asset";

            string[] bootGuids = AssetDatabase.FindAssets(
         "RR_Boot t:Scene",
         new[] { "Assets/Samples/Reality Roost SDK" }
     );

            string[] calibGuids = AssetDatabase.FindAssets(
                "RR_Calib t:Scene",
                new[] { "Assets/Samples/Reality Roost SDK" }
            );

            // Check that Required Boot Scenes sample was imported
            if (bootGuids.Length == 0 || calibGuids.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Required Boot Scenes Missing",
                    "Please import the 'Required Boot Scenes' sample before adding the RR-Host Build Profile.",
                    "OK"
    );
                return;
            }

            string rrBootScenePath =
                AssetDatabase.GUIDToAssetPath(bootGuids[0]);

            string rrCalibScenePath =
                AssetDatabase.GUIDToAssetPath(calibGuids[0]);

            var profile =
                AssetDatabase.LoadAssetAtPath<UnityEditor.Build.Profile.BuildProfile>(
                    source
                );

            if (profile == null)
            {
                Debug.LogError("RR-Host Build Profile could not be loaded.");
                return;
            }

            profile.scenes = new[]
            {
        new EditorBuildSettingsScene(rrBootScenePath, true),
        new EditorBuildSettingsScene(rrCalibScenePath, true)
    };

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);

            Debug.Log("RR-Host scenes added successfully.");
#endif
#if UNITY_6000_6_OR_NEWER

        const string Source = "Packages/net.xpixels.reality-roost/Editor/Build Profiles/RR-Host.asset";
        const string Destination = "Assets/Settings/Build Profiles/RR-Host.asset";

        [MenuItem("Reality Roost/Add Build Profile", priority = 99)]
        public static void AddBuildProfilesToAssets()
        {
            // Create Assets/Settings/ if folder doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
            // Create Assets/Settings/Build Profiles if folder doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Build Profiles"))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Settings",
                    "Build Profiles"
                );
            }

            // Copy profile
            if (!AssetDatabase.CopyAsset(Source, Destination))
            {
                Debug.LogError("Failed to copy RR-Host.");
                return;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("RR-Host Build Profile added.");

            // Add scenes to build profile
            AddScenesToBuildProfile(Destination);
        }
        public static void AddScenesToBuildProfile(string destination)
        {
            string[] bootGuids = AssetDatabase.FindAssets(
         "RR_Boot t:Scene",
         new[] { "Assets/Samples/Reality Roost SDK" }
     );

            string[] calibGuids = AssetDatabase.FindAssets(
                "RR_Calib t:Scene",
                new[] { "Assets/Samples/Reality Roost SDK" }
            );

            // Check that Required Boot Scenes sample was imported
            if (bootGuids.Length == 0 || calibGuids.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Required Boot Scenes Missing",
                    "Please import the 'Required Boot Scenes' sample before adding the RR-Host Build Profile.",
                    "OK"
    );
                return;
            }

            string rrBootScenePath =
                AssetDatabase.GUIDToAssetPath(bootGuids[0]);

            string rrCalibScenePath =
                AssetDatabase.GUIDToAssetPath(calibGuids[0]);

            var profile =
                AssetDatabase.LoadAssetAtPath<UnityEditor.Build.Profile.BuildProfile>(
                    destination
                );

            if (profile == null)
            {
                Debug.LogError("RR-Host Build Profile could not be loaded.");
                return;
            }

            profile.scenes = new[]
            {
        new EditorBuildSettingsScene(rrBootScenePath, true),
        new EditorBuildSettingsScene(rrCalibScenePath, true)
    };

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);

            Debug.Log("RR-Host scenes added successfully.");
        }
#endif
        }
    }
}
