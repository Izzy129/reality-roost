using Gsplat.Samples;
using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Retrieve a Gaussian spalt's .PLY file through File Explorer
/// </summary>
public class RetrievePlySplat : MonoBehaviour
{
    string filePath;
    public string fileName;

    private RuntimePlyBytesLoaderExample _RuntimePly;
    void Start()
    {
        _RuntimePly = GetComponent<RuntimePlyBytesLoaderExample>();
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
            filePath = path;
            _RuntimePly.PlyPath = filePath;
        }
    }
}
