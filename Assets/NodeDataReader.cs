using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Globalization;

// Ensure the NodeData class from previous steps is accessible, 
// either in its own NodeData.cs file or defined here.
// For demonstration, including a simplified version here, 
// but it should ideally reference the one from f5033a93 or 08697d48.

// Re-defining NodeData for clarity if it's not in a separate file, ensuring it's serializable.
// If you already have NodeData from a previous step in its own file, you can remove this block.
//[System.Serializable]
//public class NodeData
//{
//    public int nodeNumber;
//    public Vector3 coordinates;
//    public float velocityMagnitude;
//    public Vector3 velocityComponents; // Vx, Vy, Vz
//    public float pressure; // Added based on new header
//    // You can add more fields here like cell-id, blood-vof, contrast-vof if needed for visualization
//}

public class NodeDataReader : MonoBehaviour
{
    [Header("File Settings")]
    public FileSelector fileSelector;

    public TextAsset dataFile; // Assign your text file here in the Inspector
    //public string filePath; // Optional: Use if loading from StreamingAssets or a specific path Assets/Streaming Assets/file2.txt

    [Header("Visualization Link")]
    public NodeVisualizer nodeVisualizer; // Assign your NodeVisualizer GameObject here

    public void Render() //changed from void Start() to this new function that gets called by render button
    {
        string filePath = fileSelector.SelectionText(); //suffix is included!

        // Automatically try to find the NodeVisualizer if not assigned
        if (nodeVisualizer == null)
        {
            nodeVisualizer = FindObjectOfType<NodeVisualizer>();
            if (nodeVisualizer == null)
            {
                Debug.LogError("NodeVisualizer not found in the scene. Please assign it or ensure it's present.");
                return;
            }
        }

        List<NodeData> loadedNodes = new List<NodeData>();

        // Option 1: Load from TextAsset (recommended for packaged games)
        if (dataFile != null)
        {
            Debug.Log($"Attempting to load data from TextAsset: {dataFile.name}");
            loadedNodes = ParseData(dataFile.text);
        }
        // Option 2: Load from filePath (useful for editor, streaming assets, or custom paths)
        else if (!string.IsNullOrEmpty(filePath))
        {
            //string fullPath = Path.Combine(Application.streamingAssetsPath, filePath);
            string fullPath = Application.streamingAssetsPath + "/" + filePath;
            if (File.Exists(fullPath))
            {
                Debug.Log($"Attempting to load data from path: {fullPath}");
                loadedNodes = ParseData(File.ReadAllText(fullPath));
            }
            else
            {
                Debug.LogError($"File not found at: {fullPath}");
            }
        }
        else
        {
            Debug.LogError("No dataFile (TextAsset) assigned and no filePath provided in NodeDataReader.");
            return;
        }

        if (loadedNodes.Count > 0)
        {
            Debug.Log($"Successfully loaded and parsed {loadedNodes.Count} nodes.");
            // Pass the parsed data to the NodeVisualizer
            nodeVisualizer.nodesToVisualize = loadedNodes;
            // Trigger visualization
            nodeVisualizer.VisualizeNodes();
        }
        else
        {
            Debug.LogWarning("No nodes were loaded or parsed. Check your data file and format.");
        }
    }

    private List<NodeData> ParseData(string fileContent)
    {
        List<NodeData> nodes = new List<NodeData>();
        StringReader reader = new StringReader(fileContent);
        string line;
        string headerLine = null;
        string[] columnNames = null;

        // Read header line
        if ((line = reader.ReadLine()) != null)
        {
            headerLine = line.Trim();
            columnNames = headerLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        if (columnNames == null || columnNames.Length == 0)
        {
            Debug.LogError("Could not parse header or header is empty.");
            return nodes;
        }

        // Map column names to their indices for easier access
        Dictionary<string, int> columnIndex = new Dictionary<string, int>();
        for (int i = 0; i < columnNames.Length; i++)
        {
            columnIndex[columnNames[i].ToLowerInvariant()] = i;
            Debug.Log("i: " + columnNames[i]);
        }

        // Check for essential columns
        if (!columnIndex.ContainsKey("nodenumber") ||
            !columnIndex.ContainsKey("x-coordinate") ||
            !columnIndex.ContainsKey("y-coordinate") ||
            !columnIndex.ContainsKey("z-coordinate") ||
            //!columnIndex.ContainsKey("velocity-magnitude") ||
            !columnIndex.ContainsKey("x-velocity") ||
            !columnIndex.ContainsKey("y-velocity") ||
            !columnIndex.ContainsKey("z-velocity"))
        {
            Debug.LogError("Missing one or more required columns (nodenumber, x-coordinate, y-coordinate, z-coordinate, velocity-magnitude, x-velocity, y-velocity, z-velocity) in the data file header.");
            return nodes;
        }

        while ((line = reader.ReadLine()) != null) //while theres still stuff to read ?
            
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != columnNames.Length)
            {
                Debug.LogWarning($"Skipping line due to column mismatch: {line}");
                continue;
            }

            try
            {
                NodeData newNode = new NodeData();

                // Parse NodeNumber
                newNode.nodeNumber = int.Parse(parts[columnIndex["nodenumber"]], CultureInfo.InvariantCulture);

                // Parse Coordinates
                float x = float.Parse(parts[columnIndex["x-coordinate"]], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[columnIndex["y-coordinate"]], CultureInfo.InvariantCulture);
                float z = float.Parse(parts[columnIndex["z-coordinate"]], CultureInfo.InvariantCulture);
                newNode.coordinates = new Vector3(x, y, z);

                // Parse Velocity Magnitude
                //newNode.velocityMagnitude = float.Parse(parts[columnIndex["velocity-magnitude"]], CultureInfo.InvariantCulture);

                // Parse Velocity Components
                float vx = float.Parse(parts[columnIndex["x-velocity"]], CultureInfo.InvariantCulture);
                float vy = float.Parse(parts[columnIndex["y-velocity"]], CultureInfo.InvariantCulture);
                float vz = float.Parse(parts[columnIndex["z-velocity"]], CultureInfo.InvariantCulture);
                newNode.velocityComponents = new Vector3(vx, vy, vz);
                newNode.velocityMagnitude = Vector3.Magnitude(newNode.velocityComponents);

                // Parse Pressure if available (new field)
                if (columnIndex.ContainsKey("pressure"))
                {
                    newNode.pressure = float.Parse(parts[columnIndex["pressure"]], CultureInfo.InvariantCulture);
                }

                nodes.Add(newNode);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing line: {line}. Error: {e.Message}");
            }
        }

        return nodes;
    }
}