using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class TileMapper : MonoBehaviour
{
    // This singleton is responsible for providing an API for GameObjects to use to rumble a tile based on their position
    #region Singleton
    private static TileMapper _instance;
    public static TileMapper Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TileMapper>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TileMapper");
                    _instance = go.AddComponent<TileMapper>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    #endregion

    #region Inspector Fields
    [Header("OSC Sender Reference")]
    [SerializeField] private TileOSCSender tileOSCSender;

    [Header("Tile GameObjects")]
    [SerializeField] public GameObject[] tileGOs;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    #endregion

    #region Physical Constants
    private const float TILE_SIZE = 0.9144f;      // 36 inches in meters
    private const float TILE_SPACING = 0.0127f;   // Spacing between tiles
    private const int GRID_SIZE = 4;              // 4x4 grid
    // Stride = distance from center to center of adjacent tiles
    private const float STRIDE = TILE_SIZE + TILE_SPACING;
    #endregion

    #region State
    // Tracks the last active tile index for each caller to handle resetting colors when a GO moves away from a tile
    private Dictionary<Transform, int> _activeTileMap = new Dictionary<Transform, int>();
    #endregion

    #region Public API
    /// <summary>
    /// Rumbles the tile at the caller's position with the specified intensity.
    /// Intensity is clamped between 0 and 1.
    /// </summary>
    /// <param name="caller">Transform of the GameObject calling this method</param>
    /// <param name="intensity">Rumble intensity (0-1)</param>
    public void Rumble(Transform caller, float intensity)
    {
        if (caller == null)
        {
            Debug.LogError("[TileMapper] Rumble called with null Transform!");
            return;
        }

        if (tileOSCSender == null)
        {
            Debug.LogError("[TileMapper] TileOSCSender reference is not assigned in Inspector!");
            return;
        }
        
        // Clamp intensity to valid range
        intensity = Mathf.Clamp01(intensity);

        // Get caller's position
        Vector3 position = caller.position;

        // Map position to tile coordinates
        int arrayIndex = WorldPositionToTile(position);

        // Check if position is out of bounds
        if (arrayIndex == -1)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[TileMapper] GameObject '{caller.name}' is out of bounds. Ignoring rumble request.");
            }
            return;
        }

        // handle visuals of tile that GO was previously on
        if (_activeTileMap.ContainsKey(caller))
        {
            int previousIndex = _activeTileMap[caller];
            if (previousIndex != arrayIndex)
            {
                // caller moved to a new tile; reset old tile
                SetTileColor(previousIndex, Color.gray);
            }
        }
        _activeTileMap[caller] = arrayIndex;

        // Create fresh intensity array (all zeros)
        float[] intensities = new float[16];

        // Set only the target tile's intensity
        intensities[arrayIndex] = intensity;
        SetTileColor(arrayIndex, Color.red);

        // Send to OSC
        tileOSCSender.SendIntensities(intensities);

        if (showDebugLogs)
        {
            Debug.Log($"[TileMapper] Rumble: '{caller.name}' [Index {arrayIndex}] with intensity {intensity:F2}");
        }
    }
    #endregion

    #region Helper Methods
    private void SetTileColor(int index, Color color)
    {
        if (tileGOs != null && index >= 0 && index < tileGOs.Length && tileGOs[index] != null)
        {
            Renderer rend = tileGOs[index].GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.SetColor("_BaseColor", color);
            }
        }
    }

    /// <summary>
    /// Converts world position to tile index (0-15).
    /// Snaps to the nearest tile.
    /// </summary>
    private int WorldPositionToTile(Vector3 worldPos)
    { 
        // MATH:
        // x = -offset + col * stride
        // z = offset - row * stride
        // offset = 1.5 * stride
        
        // col = (x / stride) + 1.5
        // row = 1.5 - (z / stride)

        int col = Mathf.RoundToInt((worldPos.x / STRIDE) + 1.5f);
        int row = Mathf.RoundToInt(1.5f - (worldPos.z / STRIDE));

        // clamp to grid bounds (snapping behavior; edge case where object is between tiles)
        col = Mathf.Clamp(col, 0, GRID_SIZE - 1);
        row = Mathf.Clamp(row, 0, GRID_SIZE - 1);

        return row * GRID_SIZE + col;
    }
    #endregion
}