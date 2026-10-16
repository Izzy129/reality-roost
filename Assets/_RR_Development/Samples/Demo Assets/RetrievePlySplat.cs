using Gsplat;
using Gsplat.Samples;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Retrieve a Gaussian spalt's .PLY file through File Explorer
/// </summary>
public class RetrievePlySplat : MonoBehaviour
{
    string filePath;
    public string fileName;
    private bool _foundFile = false;

    private RuntimePlyBytesLoaderExample _RuntimePly;
    private GsplatRenderer _GsplatRenderer;
    void Awake()
    {
        _RuntimePly = GetComponent<RuntimePlyBytesLoaderExample>();
        _GsplatRenderer = GetComponent<GsplatRenderer>();
        filePath = @"\\DXP4800-99FE\Rig Captures\Reality Roost Splats\" + fileName;
        FindPlyFile(filePath);
    }

    /// <summary>
    /// Find Gaussian Splat's .ply file through file system
    /// </summary>
    /// <param name="path"></param>
    private void FindPlyFile(string path)
    {
        if (!File.Exists(path))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents",
                "Reality Roost Development",
                "Splats",
                fileName
            );
        }
        
        if(File.Exists(path))
        {
            Debug.Log("Found .ply file " + path);
            _foundFile = true;
            filePath = path;
            _RuntimePly.PlyPath = filePath;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Renders the splat in-editor via Gizmos. Does not render during Play mode.
    /// </summary>
    private void OnDrawGizmos()
    {
        if(!EditorApplication.isPlayingOrWillChangePlaymode) 
        {
            if (!_foundFile)
            {
                // Get references
                _RuntimePly = GetComponent<RuntimePlyBytesLoaderExample>();
                _GsplatRenderer = GetComponent<GsplatRenderer>();
                // Find file path
                filePath = @"\\DXP4800-99FE\Rig Captures\Reality Roost Splats\" + fileName;
                FindPlyFile(filePath);
                // Render found file path
                if(_foundFile)
                {
                    var bytes = File.ReadAllBytes(filePath);
                    _GsplatRenderer.GsplatAsset = ScriptableObject.CreateInstance<GsplatAssetSpark>();
                    _GsplatRenderer.GsplatAsset.LoadFromPlyBytes(bytes, null, _RuntimePly.SourceCoordinates);
                }
            }
        }
        
    }
#endif
}
