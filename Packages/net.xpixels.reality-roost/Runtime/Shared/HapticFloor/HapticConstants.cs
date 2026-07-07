namespace RealityRoost.Shared.HapticFloor
{
    public static class HapticConstants
    {
        public const int GRID_COLS = 2; // along X
        public const int GRID_ROWS = 3; // along Z
        public const int TILE_COUNT = GRID_COLS * GRID_ROWS;
        public const float TILE_SIZE = 0.9144f; // 36 inches in meters
        public const float TILE_SPACING = 0.0127f; // 0.5 inches in meters
    }
}
