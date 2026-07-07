using UnityEngine;

namespace RealityRoost.Shared.HapticFloor
{
    public static class HapticFloorUtils
    {
        public static int PositionToTileIndex(Vector3 worldPosition)
        {
            const float stride = HapticConstants.TILE_SIZE + HapticConstants.TILE_SPACING;
            int col = Mathf.RoundToInt((worldPosition.x / stride) + (HapticConstants.GRID_COLS - 1) / 2f);
            int row = Mathf.RoundToInt((HapticConstants.GRID_ROWS - 1) / 2f - (worldPosition.z / stride));

            col = Mathf.Clamp(col, 0, HapticConstants.GRID_COLS - 1);
            row = Mathf.Clamp(row, 0, HapticConstants.GRID_ROWS - 1);

            int index = row * HapticConstants.GRID_COLS + col;
            return Mathf.Clamp(index, 0, HapticConstants.TILE_COUNT - 1);
        }

        public static bool IsValidTileIndex(int tileIndex, string caller)
        {
            if (tileIndex < 0 || tileIndex >= HapticConstants.TILE_COUNT)
            {
                Debug.LogError($"[RR][ERROR] {caller}: tileIndex {tileIndex} out of range [0, {HapticConstants.TILE_COUNT}).");
                return false;
            }

            return true;
        }

        public static float ClampIntensity(float intensity, string caller)
        {
            if (intensity < 0f || intensity > 1f)
            {
                Debug.LogWarning($"[RR][WARN] {caller}: intensity {intensity} out of range [0.0, 1.0]. Clamping and proceeding...");
            }

            return Mathf.Clamp01(intensity);
        }
    }
}
