import math

# Prefab GUID
PREFAB_GUID = "8ec5c01b9c2af2049b6800fe979ee037"

# Floor Transform IDs
floor_transform = {
    1: 9000000002, 
    2: 9000000004, 
    3: 9000000006, 
    4: 9000000008, 
    5: 9000000010
}

# 5층 구조
floors = [
    (1, 0, "1Floor"),
    (2, 5, "2Floor"),
    (3, 10, "3Floor"),
    (4, 15, "4Floor"),
    (5, 20, "5Floor"),
]

# Generate tile data
spacing = 1.3
radius = 3
tiles = []
tile_counter = 1

for floor_num, y_pos, floor_name in floors:
    for i in range(18):
        angle = i * 20 * math.pi / 180
        x = round(math.cos(angle) * spacing * radius, 4)
        z = round(math.sin(angle) * spacing * radius, 4)
        tiles.append((x, y_pos, z, tile_counter, floor_num))
        tile_counter += 1

print(f"Total tiles: {len(tiles)}")

# Generate Floor objects
floor_content = []
for floor_num, y_pos, floor_name in floors:
    obj_id = 9000000001 + (floor_num - 1) * 2
    transform_id = obj_id + 1
    floor_content.append(f"""--- !u!1 &{obj_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {transform_id}}}
  m_Layer: 0
  m_Name: {floor_num}Floor
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {obj_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 399977391}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
""")

# Generate HexTile PrefabInstance objects
tile_content = []
hex_tiles_list = []

for idx, (x, y, z, tile_num, floor_num) in enumerate(tiles):
    # IDs
    prefab_instance_id = 1000000000 + idx * 100
    gameobject_id = prefab_instance_id + 1
    transform_id = prefab_instance_id + 2
    meshfilter_id = prefab_instance_id + 3
    meshrenderer_id = prefab_instance_id + 4
    boxcollider_id = prefab_instance_id + 5
    hextile_id = prefab_instance_id + 6
    
    parent_id = floor_transform[floor_num]
    tile_name = f"hextile{tile_num:02d}"
    
    # Complete PrefabInstance with GameObject and Transform
    tile_content.append(f"""--- !u!1001 &{prefab_instance_id}
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_CorrespondingSourceObject: {{fileID: 7018648038695141315, guid: {PREFAB_GUID}, type: 3}}
  m_PrefabInstance: {{fileID: 0}}
  m_GameObject: {{fileID: {gameobject_id}}}
  m_Enabled: 1
  m_Component:
  - component: {{fileID: {transform_id}}}
  - component: {{fileID: {meshfilter_id}}}
  - component: {{fileID: {meshrenderer_id}}}
  - component: {{fileID: {boxcollider_id}}}
  - component: {{fileID: {hextile_id}}}
  m_Layer: 0
  m_Name: {tile_name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!1 &{gameobject_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 7018648038695141315, guid: {PREFAB_GUID}, type: 3}}
  m_PrefabInstance: {{fileID: {prefab_instance_id}}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {transform_id}}}
  m_Layer: 0
  m_Name: HexTile
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 6394647473022444641, guid: {PREFAB_GUID}, type: 3}}
  m_PrefabInstance: {{fileID: {prefab_instance_id}}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {gameobject_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: -0, y: -0, z: -0, w: 1}}
  m_LocalPosition: {{x: {x}, y: {y}, z: {z}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {parent_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
""")
    
    hex_tiles_list.append(f"  - {{fileID: {hextile_id}}}")

# Write files
with open(r"C:\PartyWorld\floors.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(floor_content))

with open(r"C:\PartyWorld\tiles.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(tile_content))

with open(r"C:\PartyWorld\hex_list.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(hex_tiles_list))

print(f"Generated: {len(floor_content)} floors + {len(tiles)} PrefabInstances")
print("Each PrefabInstance has: PrefabInstance + GameObject + Transform")
