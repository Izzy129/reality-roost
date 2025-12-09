using UnityEngine;

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
    [SerializeField] private TileIntensityOSC tileIntensityOSC;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    #endregion

    #region Physical Constants
    private const float TILE_SIZE = 0.9144f;      // 36 inches in meters
    private const float TILE_SPACING = 0.0127f;   // Spacing between tiles
    private const int GRID_SIZE = 4;              // 4x4 grid
    private const float FIRST_TILE_OFFSET = 1.39065f; // Distance from origin to first tile center
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

        if (tileIntensityOSC == null)
        {
            Debug.LogError("[TileMapper] TileIntensityOSC reference is not assigned in Inspector!");
            return;
        }

        // Clamp intensity to valid range
        intensity = Mathf.Clamp01(intensity);

        // Get caller's position
        Vector3 position = caller.position;

        // Map position to tile coordinates
        (int row, int col) = WorldPositionToTile(position);

        // Check if position is out of bounds
        if (row < 0 || col < 0)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[TileMapper] GameObject '{caller.name}' at position ({position.x:F3}, {position.z:F3}) is out of bounds. Ignoring rumble request.");
            }
            return;
        }

        // Create fresh intensity array (all zeros)
        float[] intensities = new float[16];

        // Set only the target tile's intensity
        int arrayIndex = TileToArrayIndex(row, col);
        intensities[arrayIndex] = intensity;

        // Send to OSC
        tileIntensityOSC.SendIntensities(intensities);

        if (showDebugLogs)
        {
            Debug.Log($"[TileMapper] Rumble: '{caller.name}' → Tile ({row}, {col}) [Index {arrayIndex}] with intensity {intensity:F2}");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Converts world position to tile coordinates (row, col).
    /// Returns (-1, -1) if position is outside the floor bounds.
    /// </summary>
    private (int row, int col) WorldPositionToTile(Vector3 worldPos)
    {
        // Calculate which column (X axis)
        float offsetX = worldPos.x + FIRST_TILE_OFFSET;
        if (offsetX < 0) return (-1, -1);
        int col = Mathf.FloorToInt(offsetX / (TILE_SIZE + TILE_SPACING));

        // Calculate which row (Z axis, negated)
        float offsetZ = FIRST_TILE_OFFSET - worldPos.z;
        if (offsetZ < 0) return (-1, -1);
        int row = Mathf.FloorToInt(offsetZ / (TILE_SIZE + TILE_SPACING));

        // Validate bounds
        if (row < 0 || row >= GRID_SIZE || col < 0 || col >= GRID_SIZE)
            return (-1, -1);

        return (row, col);
    }

    /// <summary>
    /// Converts (row, col) to 1D array index for OSC transmission (0-15).
    /// </summary>
    private int TileToArrayIndex(int row, int col)
    {
        return row * GRID_SIZE + col;
    }
    #endregion
}