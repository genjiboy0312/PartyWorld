import re

# Read generated files
with open(r"C:\PartyWorld\floors_output.txt", "r", encoding="utf-8") as f:
    floors_and_tiles = f.read()

with open(r"C:\PartyWorld\hextiles_output.txt", "r", encoding="utf-8") as f:
    hex_tiles_list = f.read()

# Read original scene
with open(r"C:\PartyWorld\Assets\GameData\Scenes\Scene_HexagonMap.unity", "r", encoding="utf-8") as f:
    scene_content = f.read()

# 1. Remove ALL HexTile entries (object IDs starting with 1000000xxx and 9000000xxx)
# Pattern to match any HexTile object entries
pattern_hex = r'--- !u!1 &1[0-9]{9}\n.*?(?=\n--- !u!1 &|\n--- !u!4 &9[0-9]{9}|\n--- !u!1660057539)'
scene_content = re.sub(pattern_hex, '', scene_content, flags=re.DOTALL)

pattern_hex2 = r'--- !u!1 &9[0-9]{9}\n.*?(?=\n--- !u!1 &|\n--- !u!1660057539)'
scene_content = re.sub(pattern_hex2, '', scene_content, flags=re.DOTALL)

# 2. Remove Floor objects (9000000xxx)
pattern_floor = r'--- !u!1 &9[0-9]{9}\n.*?(?=\n--- !u!1 &|\n--- !u!1660057539)'
scene_content = re.sub(pattern_floor, '', scene_content, flags=re.DOTALL)

# 3. Update HexArenaManager _hexTiles list
scene_content = re.sub(
    r'_hexTiles:\n.*?(?=\n  _tileContainer:)',
    f'_hexTiles:\n{hex_tiles_list}',
    scene_content,
    flags=re.DOTALL
)

# 4. Remove HexArenaGenerator component if exists
scene_content = re.sub(
    r'--- !u!114 &1865829111\nMonoBehaviour:\n.*?m_EditorClassIdentifier: Assembly-CSharp::HexArenaGenerator\n.*?(?=\n--- !u!1 &)',
    '',
    scene_content,
    flags=re.DOTALL
)

# Remove HexArenaGenerator from component list
scene_content = re.sub(
    r'(\n  m_Component:\n)(    - component: \{fileID: 1865829110\}\n    - component: \{fileID: 1865829111\}\n)',
    r'\1    - component: {fileID: 1865829110}\n',
    scene_content
)

# 5. Add floors and tiles before SceneRoots
scene_content = re.sub(
    r'(\n--- !u!1660057539 &9223372036854775807)',
    f'\n{floors_and_tiles}\n\\1',
    scene_content
)

# 6. Update HexArenaManager to reference tile container
# Ensure _tileContainer points to HexTileContainer
scene_content = re.sub(
    r'_tileContainer: \{fileID: 399977391\}',
    '_tileContainer: {fileID: 399977391}',
    scene_content
)

# Write updated scene
with open(r"C:\PartyWorld\Assets\GameData\Scenes\Scene_HexagonMap.unity", "w", encoding="utf-8") as f:
    f.write(scene_content)

print("Scene updated!")
print("- 5 Floor parents (1Floor ~ 5Floor)")
print("- 61 HexTiles (hextile01 ~ hextile61)")
print("- HexArenaManager._hexTiles updated")
print("- HexArenaGenerator removed")
