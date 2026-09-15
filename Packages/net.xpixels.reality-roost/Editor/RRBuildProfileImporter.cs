using UnityEngine;
using UnityEditor;

namespace RealityRoost.Editor
{
    public static class RRBuildProfileImporter
    {
#if UNITY_6000_6_OR_NEWER

        const string Source = "Packages/Reality Roost SDK/Editor/Build Profiles/RR-Host.asset";
        const string DestinationFolder = "Assets/Settings/Build Profiles";

        [MenuItem("Add Build Profile", priority = 99)]
        public static void AddBuildProfilesToAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
            // Create Assets/Settings/Build Profiles if needed
            if (!AssetDatabase.IsValidFolder("Assets/Settings/Build Profiles"))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Settings",
                    "Build Profiles"
                );
            }

            // Copy profile
            if (!AssetDatabase.CopyAsset(Source, DestinationFolder))
            {
                Debug.LogError("Failed to copy RR-Host.");
                return;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("RR-Host Build Profile added.");
        }
#endif
    }
}
