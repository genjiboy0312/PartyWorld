using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CharacterCatalogTools
{
    private const string CatalogAssetPath = "Assets/Resources/CharacterCatalog.asset";
    private const string LegacyCatalogAssetPath = "Assets/GameData/Data/CharacterCatalog.asset";
    private const string CharacterPrefabFolder = "Assets/GameData/Prefabs/Characters";

    [MenuItem("GenJiTools/Character Catalog/Sync Catalog")]
    private static void SyncCatalog()
    {
        CharacterCatalog catalog = LoadOrCreateCatalog();
        if (catalog == null)
            return;

        List<CharacterCatalog.Entry> previous = catalog.Entries;
        Dictionary<string, string> existingIdByPrefabPath = new Dictionary<string, string>();

        for (int i = 0; i < previous.Count; i++)
        {
            CharacterCatalog.Entry entry = previous[i];
            if (entry.prefab == null)
                continue;

            string path = AssetDatabase.GetAssetPath(entry.prefab);
            if (string.IsNullOrWhiteSpace(path))
                continue;

            if (!existingIdByPrefabPath.ContainsKey(path))
                existingIdByPrefabPath.Add(path, entry.characterId);
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { CharacterPrefabFolder });
        List<string> prefabPaths = new List<string>();
        for (int i = 0; i < guids.Length; i++)
            prefabPaths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));

        prefabPaths.Sort();

        List<CharacterCatalog.Entry> synced = new List<CharacterCatalog.Entry>();
        for (int i = 0; i < prefabPaths.Count; i++)
        {
            string path = prefabPaths[i];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            string id;
            if (!existingIdByPrefabPath.TryGetValue(path, out id) || string.IsNullOrWhiteSpace(id))
                id = prefab.name;

            CharacterCatalog.Entry entry = new CharacterCatalog.Entry
            {
                characterId = id,
                prefab = prefab
            };

            synced.Add(entry);
        }

        SerializedObject so = new SerializedObject(catalog);
        SerializedProperty entriesProp = so.FindProperty("_entries");
        entriesProp.arraySize = synced.Count;

        for (int i = 0; i < synced.Count; i++)
        {
            SerializedProperty item = entriesProp.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("characterId").stringValue = synced[i].characterId;
            item.FindPropertyRelative("prefab").objectReferenceValue = synced[i].prefab;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CharacterCatalogTools] Sync complete. entries={synced.Count}");
        Selection.activeObject = catalog;
    }

    [MenuItem("GenJiTools/Character Catalog/Validate Catalog")]
    private static void ValidateCatalog()
    {
        CharacterCatalog catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogAssetPath);
        if (catalog == null)
        {
            Debug.LogWarning("[CharacterCatalogTools] CharacterCatalog.asset not found.");
            return;
        }

        HashSet<string> ids = new HashSet<string>();
        bool valid = true;

        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            CharacterCatalog.Entry entry = catalog.Entries[i];

            if (string.IsNullOrWhiteSpace(entry.characterId))
            {
                Debug.LogWarning($"[CharacterCatalogTools] Empty characterId at index {i}.", catalog);
                valid = false;
            }

            if (entry.prefab == null)
            {
                Debug.LogWarning($"[CharacterCatalogTools] Missing prefab at index {i}.", catalog);
                valid = false;
            }

            if (!string.IsNullOrWhiteSpace(entry.characterId) && !ids.Add(entry.characterId))
            {
                Debug.LogWarning($"[CharacterCatalogTools] Duplicate characterId: {entry.characterId}", catalog);
                valid = false;
            }
        }

        if (valid)
            Debug.Log("[CharacterCatalogTools] Catalog is valid.");
    }

    private static CharacterCatalog LoadOrCreateCatalog()
    {
        CharacterCatalog catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogAssetPath);
        if (catalog != null)
            return catalog;

        CharacterCatalog legacy = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(LegacyCatalogAssetPath);
        if (legacy != null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            string moveError = AssetDatabase.MoveAsset(LegacyCatalogAssetPath, CatalogAssetPath);
            if (string.IsNullOrWhiteSpace(moveError))
            {
                AssetDatabase.SaveAssets();
                catalog = AssetDatabase.LoadAssetAtPath<CharacterCatalog>(CatalogAssetPath);
                if (catalog != null)
                    return catalog;
            }
            else
            {
                Debug.LogWarning($"[CharacterCatalogTools] Catalog move failed: {moveError}");
            }
        }

        string folder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Resources");

        catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
        AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CharacterCatalogTools] Created catalog: {CatalogAssetPath}");
        return catalog;
    }
}
