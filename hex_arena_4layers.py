# Hexagon Arena Generator - Layer 3 + 4겹 바깥 = 150 tiles

import math

def generate_hex_layer(radius):
    """Generate hex tile positions for a given radius"""
    tiles = []
    if radius == 0:
        tiles.append((0, 0))
    else:
        for q in range(-radius, radius + 1):
            r1 = max(-q - radius, -radius)
            r2 = min(-q + radius, radius)
            for r in range(r1, r2 + 1):
                tiles.append((q, r))
    return tiles

# Hexagon constants
HEX_SIZE = 0.5  # Radius of each hex
TILE_SPACING = 1.354  # Distance between hex centers (sqrt(3) * HEX_SIZE * 0.781 * 2)
TILE_SPACING = 1.3    # Slightly adjusted for closer fit

# Layer 3 center + 4 layers outward = radius 7
CENTER_LAYER = 3
OUTER_LAYERS = 4
MAX_RADIUS = CENTER_LAYER + OUTER_LAYERS  # 7

print(f"Generating Hexagon Arena:")
print(f"  Center Layer: {CENTER_LAYER} (radius {CENTER_LAYER})")
print(f"  Outer Layers: {OUTER_LAYERS}")
print(f"  Max Radius: {MAX_RADIUS}")
print()

# Generate all tiles
all_tiles = []
for radius in range(CENTER_LAYER, MAX_RADIUS + 1):
    layer_tiles = generate_hex_layer(radius)
    print(f"Layer {radius}: {len(layer_tiles)} tiles")
    all_tiles.extend([(q, r, radius) for q, r in layer_tiles])

print(f"\nTotal tiles: {len(all_tiles)}")

# Convert axial to world position
def axial_to_world(q, r):
    x = TILE_SPACING * (q + r/2.0)
    z = TILE_SPACING * (r * math.sqrt(3)/2)
    y = 0
    return (x, y, z)

# Generate positions
tile_positions = []
for q, r, layer in all_tiles:
    x, y, z = axial_to_world(q, r)
    tile_positions.append((q, r, layer, x, y, z))

# Sort by layer (inner first), then by position
tile_positions.sort(key=lambda t: (t[2], t[0], t[1]))

print(f"\nTile positions (first 20):")
for i, (q, r, layer, x, y, z) in enumerate(tile_positions[:20]):
    print(f"  {i+1}: Layer{layer} q={q}, r={r} -> pos=({x:.4f}, {y}, {z:.4f})")

print(f"\nTile positions (last 20):")
for i, (q, r, layer, x, y, z) in enumerate(tile_positions[-20:]):
    print(f"  {len(tile_positions)-19+i}: Layer{layer} q={q}, r={r} -> pos=({x:.4f}, {y}, {z:.4f})")

# Save to file
with open('hex_arena_output.txt', 'w') as f:
    f.write(f"# Hexagon Arena - Layer {CENTER_LAYER} + {OUTER_LAYERS} outer layers\n")
    f.write(f"# Total tiles: {len(tile_positions)}\n")
    f.write(f"# Tile spacing: {TILE_SPACING}\n\n")
    for i, (q, r, layer, x, y, z) in enumerate(tile_positions):
        f.write(f"{i},{q},{r},{layer},{x:.6f},{y},{z:.6f}\n")

print(f"\nSaved to hex_arena_output.txt")
