using UnityEditor;
using UnityEngine;

namespace RealityRoost.Editor
{
    /// <summary>
    /// Creates a custom effect script from the Floor Player starter template.
    /// </summary>
    internal static class FloorEffectScriptCreator
    {
        private const string TemplateGuid = "620d92d18c0649adbf845bef520ad7cc";

        [MenuItem("Assets/Create/Reality Roost/Floor Effect Script", false, 80)]
        internal static void Create()
        {
            var path = AssetDatabase.GUIDToAssetPath(TemplateGuid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[RR] Floor Effect script template could not be found.");
                return;
            }
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(path, "CustomFloorEffect.cs");
        }
    }
}
