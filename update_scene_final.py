import re

# Read generated files
with open(r"C:\PartyWorld\floors.txt", "r", encoding="utf-8") as f:
    floors = f.read()

with open(r"C:\PartyWorld\tiles.txt", "r", encoding="utf-8") as f:
    tiles = f.read()

with open(r"C:\PartyWorld\hex_list.txt", "r", encoding="utf-8") as f:
    hex_list = f.read()

# Read scene
with open(r"C:\PartyWorld\Assets\GameData\Scenes\Scene_HexagonMap.unity", "r", encoding="utf-8") as f:
    scene = f.read()

# 1. Update _hexTiles list
pattern_hex = r'_hexTiles:\n.*?(?=\n  _tileContainer:)'
scene = re.sub(pattern_hex, f'_hexTiles:\n{hex_list}', scene, flags=re.DOTALL)

# 2. Update SceneRoots
pattern_roots = r'm_Roots:\n  - \{fileID: 885549161\}\n  - \{fileID: 247249175\}\n  - \{fileID: 1160353021\}\n  - \{fileID: 399977391\}\n'
replacement = r'm_Roots:\n  - {fileID: 885549161}\n  - {fileID: 247249175}\n  - {fileID: 1160353021}\n  - {fileID: 399977391}\n  - {fileID: 9000000001}\n  - {fileID: 9000000003}\n  - {fileID: 9000000005}\n  - {fileID: 9000000007}\n  - {fileID: 9000000009}\n'
scene = re.sub(pattern_roots, replacement, scene)

# 3. Remove old floors and tiles, add new ones
# Find the pattern from floors to SceneRoots
old_pattern = r'--- !u!1 &9000000\d+\n.*?(?=--- !u!1660057539)'
scene = re.sub(old_pattern, floors + "\n" + tiles, scene, flags=re.DOTALL)

# Write
with open(r"C:\PartyWorld\Assets\GameData\Scenes\Scene_HexagonMap.unity", "w", encoding="utf-8") as f:
    f.write(scene)

print("Scene updated!")
print("- 5 Floor parents")
print("- 90 HexTile PrefabInstances with Transform")
print("- HexArenaManager._hexTiles updated")
