using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace RealityRoost.Editor
{
    // Imports the package samples the Reality Roost SDK expects into Assets/Samples/.
    //
    // These cannot be declared in package.json: UPM dependencies pull in packages, but a package's
    // *samples* are opt-in files copied into the project, and no package can import another
    // package's samples on its own. This menu item is the substitute.
    //
    // Safe to re-run - already-imported samples are skipped unless you force a reimport.
    public static class RRSampleImporter
    {
        // Package name -> sample display names, exactly as they appear in the Package Manager
        // Samples tab. Keep in sync with the versions pinned in package.json.
        private static readonly Dictionary<string, string[]> RequiredSamples = new()
        {
            {
                "com.unity.xr.interaction.toolkit", new[]
                {
                    "Starter Assets",
                    "Hands Interaction Demo",
                    "XR Interaction Simulator",
                    "Spatial Keyboard"
                }
            },
            {
                "com.unity.xr.hands", new[]
                {
                    "HandVisualizer"
                }
            }
        };

        [MenuItem("Reality Roost/Import Required Samples", priority = 100)]
        public static void ImportRequiredSamples()
        {
            Import(false);
        }

        [MenuItem("Reality Roost/Import Required Samples (force reimport)", priority = 101)]
        public static void ForceImportRequiredSamples()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reimport Reality Roost samples?",
                    "This overwrites the sample folders under Assets/Samples/. Any edits you made " +
                    "to those files will be lost.",
                    "Reimport", "Cancel"))
            {
                return;
            }
            Import(true);
        }

        private static void Import(bool force)
        {
            int imported = 0;
            int skipped = 0;
            int missing = 0;

            foreach ((string packageName, string[] wanted) in RequiredSamples)
            {
                // Empty version string resolves to the version currently installed.
                List<Sample> available = Sample.FindByPackage(packageName, string.Empty)?.ToList();
                if (available == null || available.Count == 0)
                {
                    Debug.LogError($"[RR][ERROR] SampleImporter: package '{packageName}' is not " +
                                   "installed, so its samples cannot be imported. Check that the " +
                                   "Reality Roost SDK's dependencies resolved in Package Manager.");
                    missing += wanted.Length;
                    continue;
                }

                foreach (string name in wanted)
                {
                    Sample sample = available.FirstOrDefault(
                        s => string.Equals(s.displayName, name, System.StringComparison.OrdinalIgnoreCase));

                    if (sample.Equals(default(Sample)))
                    {
                        Debug.LogError($"[RR][ERROR] SampleImporter: '{packageName}' has no sample " +
                                       $"named '{name}'. It may have been renamed in this package " +
                                       "version - open Package Manager > Samples and import it by hand.");
                        missing++;
                        continue;
                    }

                    if (sample.isImported && !force)
                    {
                        skipped++;
                        continue;
                    }

                    bool ok = sample.Import(force
                        ? Sample.ImportOptions.OverridePreviousImports
                        : Sample.ImportOptions.None);

                    if (ok)
                    {
                        Debug.Log($"[RR][INFO] SampleImporter: imported '{name}' from {packageName}.");
                        imported++;
                    }
                    else
                    {
                        Debug.LogError($"[RR][ERROR] SampleImporter: failed to import '{name}' from " +
                                       $"{packageName}. Import it manually from Package Manager > Samples.");
                        missing++;
                    }
                }
            }

            AssetDatabase.Refresh();

            string summary = $"{imported} imported, {skipped} already present, {missing} failed.";
            if (missing > 0)
            {
                Debug.LogError($"[RR][ERROR] SampleImporter: {summary} See the errors above.");
            }
            else
            {
                Debug.Log($"[RR][INFO] SampleImporter: {summary}");
            }
        }

        // TMP's resources ship inside a .unitypackage, so Unity always shows its own import
        // dialog - this can open it but cannot click through it for you.
        [MenuItem("Reality Roost/Import TMP Essentials", priority = 102)]
        public static void ImportTMPEssentials()
        {
            Debug.Log("[RR][INFO] SampleImporter: opening the TMP importer - click 'Import' in the " +
                      "dialog. Repeat with 'Import TMP Examples and Extras' if you want the examples.");
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        }
    }
}
