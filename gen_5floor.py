import math

spacing = 1.3  # Hex 간격

# 5층 구조 정의
floors = [
    (1, 0, "1Floor"),   # floor_num, layer, name
    (2, 1, "2Floor"),
    (3, 2, "3Floor"),
    (4, 3, "4Floor"),
    (5, 4, "5Floor"),
]

tiles = []
tile_counter = 1

for floor_num, layer, floor_name in floors:
    if layer == 0:
        # Layer 0: 1개 (중심)
        tiles.append((0.0, 0.0, tile_counter, floor_num, floor_name))
        tile_counter += 1
    elif layer == 1:
        # Layer 1: 6개
        for i in range(6):
            angle = i * 60 * math.pi / 180
            x = round(math.cos(angle) * spacing * 1, 4)
            z = round(math.sin(angle) * spacing * 1, 4)
            tiles.append((x, z, tile_counter, floor_num, floor_name))
            tile_counter += 1
    elif layer == 2:
        # Layer 2: 12개
        for i in range(12):
            angle = i * 30 * math.pi / 180
            x = round(math.cos(angle) * spacing * 2, 4)
            z = round(math.sin(angle) * spacing * 2, 4)
            tiles.append((x, z, tile_counter, floor_num, floor_name))
            tile_counter += 1
    elif layer == 3:
        # Layer 3: 18개
        for i in range(18):
            angle = i * 20 * math.pi / 180
            x = round(math.cos(angle) * spacing * 3, 4)
            z = round(math.sin(angle) * spacing * 3, 4)
            tiles.append((x, z, tile_counter, floor_num, floor_name))
            tile_counter += 1
    elif layer == 4:
        # Layer 4: 24개
        for i in range(24):
            angle = i * 15 * math.pi / 180
            x = round(math.cos(angle) * spacing * 4, 4)
            z = round(math.sin(angle) * spacing * 4, 4)
            tiles.append((x, z, tile_counter, floor_num, floor_name))
            tile_counter += 1

print(f"Total tiles: {len(tiles)}")

# Floor 부모 오브젝트 ID
floor_ids = {
    1: 9000000001,
    2: 9000000002,
    3: 9000000003,
    4: 9000000004,
    5: 9000000005,
}

# HexTile 오브젝트 ID (1000000000부터 시작)
tile_id_start = 1000000000

# Floor 오브젝트 생성 템플릿
def gen_floor(floor_num, parent_id):
    obj_id = floor_ids[floor_num]
    transform_id = obj_id + 1
    return f"""--- !u!1 &{obj_id}
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
"""

# HexTile 생성 템플릿
def gen_tile(x, z, tile_num, floor_num):
    obj_id = tile_id_start + (tile_num - 1) * 10
    transform_id = obj_id + 1
    meshfilter_id = obj_id + 2
    meshrenderer_id = obj_id + 3
    boxcollider_id = obj_id + 4
    hextile_id = obj_id + 5
    parent_id = floor_ids[floor_num]
    tile_name = f"hextile{tile_num:02d}"
    
    return f"""--- !u!1 &{obj_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
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
--- !u!4 &{transform_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {obj_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: -0, y: -0, z: -0, w: 1}}
  m_LocalPosition: {{x: {x}, y: 0, z: {z}}}
  m_LocalScale: {{x: 1, y: 0.2, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {parent_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &{meshfilter_id}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {obj_id}}}
  m_Mesh: {{fileID: -4879333854173542351, guid: 3ca7131af177a2d4795f2bd3caabf5a4, type: 3}}
--- !u!23 &{meshrenderer_id}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {obj_id}}}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_LodBias: 0
  m_Enabled: 1
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
--- !u!65 &{boxcollider_id}
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {obj_id}}}
  m_Material: {{fileID: 0}}
  m_IsTrigger: 1
  m_Enabled: 1
  serializedVersion: 2
  m_Size: {{x: 1, y: 1, z: 1}}
  m_Center: {{x: 0, y: 0, z: 0}}
--- !u!114 &{hextile_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {obj_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 3d0296100ee2a094b8c8ac1a19568185, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _maxDurability: 3
  _sinkDelay: 2
  _sinkSpeed: 0.5
  _sinkDepth: 5
  _greenMaterial: {{fileID: 0}}
  _yellowMaterial: {{fileID: 0}}
  _orangeMaterial: {{fileID: 0}}
  _redMaterial: {{fileID: 0}}
  _audioSource: {{fileID: 0}}
  _stepSound: {{fileID: 0}}
  _sinkSound: {{fileID: 0}}
"""

# Floor 부모 생성
floor_output = []
for floor_num, layer, floor_name in floors:
    floor_output.append(gen_floor(floor_num, 399977391))

# HexTile 생성
tile_output = []
for x, z, tile_num, floor_num, floor_name in tiles:
    tile_output.append(gen_tile(x, z, tile_num, floor_num))

# HexArenaManager의 _hexTiles 리스트 생성
hex_tiles_list = []
for i in range(len(tiles)):
    obj_id = tile_id_start + i * 10
    hextile_id = obj_id + 5
    hex_tiles_list.append(f"  - {{fileID: {hextile_id}}}")

# 출력
output = []
output.append("\n".join(floor_output))
output.append("\n".join(tile_output))

with open(r"C:\PartyWorld\floors_output.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(floor_output) + "\n" + "\n".join(tile_output))

with open(r"C:\PartyWorld\hextiles_output.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(hex_tiles_list))

with open(r"C:\PartyWorld\tile_info.txt", "w", encoding="utf-8") as f:
    for x, z, tile_num, floor_num, floor_name in tiles:
        f.write(f"hextile{tile_num:02d} -> {floor_name}, pos=({x}, {z})\n")

print("Output files generated!")
print(f"- 5Floor parents with transforms")
print(f"- {len(tiles)} HexTiles (hextile01 ~ hextile{len(tiles):02d})")
