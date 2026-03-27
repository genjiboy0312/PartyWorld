using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterCatalog", menuName = "PartyWorld/Character Catalog")]
public class CharacterCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string characterId;
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> _entries = new List<Entry>();

    public List<Entry> Entries => _entries;

    public GameObject GetPrefab(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return null;

        for (int i = 0; i < _entries.Count; i++)
        {
            Entry entry = _entries[i];
            if (string.Equals(entry.characterId, characterId, StringComparison.OrdinalIgnoreCase) && entry.prefab != null)
                return entry.prefab;
        }

        return null;
    }

    public GameObject GetPrefabOrDefault(string characterId, string defaultCharacterId)
    {
        GameObject prefab = GetPrefab(characterId);
        if (prefab != null)
            return prefab;

        prefab = GetPrefab(defaultCharacterId);
        if (prefab != null)
            return prefab;

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].prefab != null)
                return _entries[i].prefab;
        }

        return null;
    }
}
