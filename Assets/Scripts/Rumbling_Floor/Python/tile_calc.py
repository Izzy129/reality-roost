# Reality Roost Tile Position Calculator
TILE_SIZE = 0.9144      # 36 inches in meters
TILE_SPACING = 0.0127   # spacing between tiles in meters
GRID_SIZE = 4 # 4x4 grid

# Distance from origin to first tile center
FIRST_TILE_OFFSET = (TILE_SIZE / 2) + (TILE_SPACING / 2) + TILE_SIZE + TILE_SPACING
print(f"FIRST_TILE_OFFSET = {FIRST_TILE_OFFSET:.5f}m\n")

def get_tile_position(tile_index):
    """
    Gets the (X, Z) position of a tile given its 1-indexed number (1-16)
    Returns: (x, z) tuple
    """
    # Convert 1-indexed to 0-indexed
    idx = tile_index - 1
    
    # Get row and column (0-indexed)
    row = idx // GRID_SIZE
    col = idx % GRID_SIZE
    
    # Calculate X and Z positions
    x = -FIRST_TILE_OFFSET + col * (TILE_SIZE + TILE_SPACING)
    z = FIRST_TILE_OFFSET - row * (TILE_SIZE + TILE_SPACING)  # NEGATED Z
    
    return (x, z)