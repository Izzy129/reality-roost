using UnityEngine;
using UnityEditor;

namespace RealityRoost.Editor
{
    public static class RRBuildProfileImporter
    {
#if UNITY_6000_6_OR_NEWER

        const string Source = "Packages/net.xpixels.reality-roost/Editor/Build Profiles/RR-Host.asset";
        const string Destination = "Assets/Settings/Build Profiles/RR-Host.asset";

        [MenuItem("Reality Roost/Add Build Profile", priority = 99)]
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
            if (!AssetDatabase.CopyAsset(Source, Destination))
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
