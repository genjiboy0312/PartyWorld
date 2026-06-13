import math

# Generate all tile data
tiles = []
tile_id = 2000000000
file_id = 2000000100

# Layer 0: 1 tile
tiles.append({'name': 'HexTile_Center', 'x': 0, 'z': 0, 'tid': tile_id, 'fid': file_id})
tile_id += 10000000; file_id += 5

# Layer 1: 6 tiles
for i in range(6):
    angle = math.radians(i * 60)
    tiles.append({'name': f'HexTile_L1_{i}', 'x': round(math.cos(angle), 3), 'z': round(math.sin(angle), 3), 'tid': tile_id, 'fid': file_id})
    tile_id += 10000000; file_id += 5

# Layer 2: 12 tiles
for i in range(12):
    angle = math.radians(i * 30)
    tiles.append({'name': f'HexTile_L2_{i}', 'x': round(math.cos(angle) * 2, 3), 'z': round(math.sin(angle) * 2, 3), 'tid': tile_id, 'fid': file_id})
    tile_id += 10000000; file_id += 5

# Layer 3: 18 tiles
for i in range(18):
    angle = math.radians(i * 20)
    tiles.append({'name': f'HexTile_L3_{i}', 'x': round(math.cos(angle) * 3, 3), 'z': round(math.sin(angle) * 3, 3), 'tid': tile_id, 'fid': file_id})
    tile_id += 10000000; file_id += 5

# Layer 4: 24 tiles
for i in range(24):
    angle = math.radians(i * 15)
    tiles.append({'name': f'HexTile_L4_{i}', 'x': round(math.cos(angle) * 4, 3), 'z': round(math.sin(angle) * 4, 3), 'tid': tile_id, 'fid': file_id})
    tile_id += 10000000; file_id += 5

# Generate YAML
with open('C:/PartyWorld/tiles_output.txt', 'w') as f:
    for t in tiles:
        f.write(f"""--- !u!1 &{t['tid']}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {t['tid'] + 1}}}
  - component: {{fileID: {t['fid'] + 3}}}
  - component: {{fileID: {t['fid'] + 2}}}
  - component: {{fileID: {t['fid'] + 1}}}
  - component: {{fileID: {t['fid']}}}
  m_Layer: 0
  m_Name: {t['name']}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{t['tid'] + 1}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {t['tid']}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {t['x']}, y: 1, z: {t['z']}}}
  m_LocalScale: {{x: 1, y: 0.2, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 399977391}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{t['fid']}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {t['tid']}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 3d0296100ee2a094b8c8ac1a19568185, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::HexTile
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
--- !u!65 &{t['fid'] + 1}
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {t['tid']}}}
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 1
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 3
  m_Size: {{x: 1, y: 1, z: 1}}
  m_Center: {{x: 0, y: 0, z: 0}}
--- !u!23 &{t['fid'] + 2}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {t['tid']}}}
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
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10303, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
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
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
--- !u!33 &{t['fid'] + 3}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {t['tid']}}}
  m_Mesh: {{fileID: -4879333854173542351, guid: 3ca7131af177a2d4795f2bd3caabf5a4, type: 3}}

""")

print(f'Generated {len(tiles)} tiles')
