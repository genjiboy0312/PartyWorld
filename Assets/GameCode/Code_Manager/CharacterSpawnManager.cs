using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[Serializable]
public struct CharacterSpawnEntry
{
    public string characterId;
    public string prefabName;
}

[DisallowMultipleComponent]
public sealed class CharacterSpawnManager : MonoBehaviour
{
    private const string PREF_SELECTED_CHARACTER_ID = "PW_SELECTED_CHARACTER_ID";
    private const string PREF_SELECTED_CHARACTER_PREFAB = "PW_SELECTED_CHARACTER_PREFAB";
    private const string CHARACTER_CATALOG_RESOURCE_PATH = "CharacterCatalog";
    private const string PLAYER_PROP_CHARACTER_ID = "characterId";

    [Header("Character Spawn")]
    [SerializeField] private CharacterCatalog _characterCatalog;
    [SerializeField] private List<CharacterSpawnEntry> _characterSpawnEntries = new List<CharacterSpawnEntry>();
    [SerializeField] private string _defaultCharacterId = "bear_base";
    [SerializeField] private string _defaultPlayerPrefabName = "Player_Test01";
    [SerializeField] private Vector3 _fallbackSpawnPosition = Vector3.zero;
    [SerializeField] private Vector3 _fallbackSpawnStep = new Vector3(1.5f, 0f, 0f);

    private GameObject _localSpawnedPlayer;
    private bool _characterCatalogTriedLoad;

    internal GameObject LocalSpawnedPlayer => _localSpawnedPlayer;

    internal void Configure(
        CharacterCatalog characterCatalog,
        List<CharacterSpawnEntry> characterSpawnEntries,
        string defaultCharacterId,
        string defaultPlayerPrefabName,
        Vector3 fallbackSpawnPosition,
        Vector3 fallbackSpawnStep)
    {
        _characterCatalog = characterCatalog;
        _characterSpawnEntries = characterSpawnEntries != null
            ? new List<CharacterSpawnEntry>(characterSpawnEntries)
            : new List<CharacterSpawnEntry>();
        _defaultCharacterId = defaultCharacterId;
        _defaultPlayerPrefabName = defaultPlayerPrefabName;
        _fallbackSpawnPosition = fallbackSpawnPosition;
        _fallbackSpawnStep = fallbackSpawnStep;
    }

    internal void SyncLocalSelectedCharacterProperty(NetworkAuthorityManager authority)
    {
        if (authority == null)
            return;

        string selectedCharacterId = ResolveSelectedCharacterId();
        if (!string.IsNullOrWhiteSpace(selectedCharacterId))
            authority.SetLocalPlayerProperty(PLAYER_PROP_CHARACTER_ID, selectedCharacterId);
    }

    internal void ResetLocalSpawnedPlayerReference()
    {
        _localSpawnedPlayer = null;
    }

    internal void CleanupLocalSpawnedPlayer()
    {
        if (_localSpawnedPlayer == null)
            return;

        PhotonView view = _localSpawnedPlayer.GetComponent<PhotonView>();
        if (view != null && view.IsMine)
            PhotonNetwork.Destroy(_localSpawnedPlayer);

        _localSpawnedPlayer = null;
    }

    internal void TrySpawnLocalPlayerForMap()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return;

        Vector3 spawnPosition = _fallbackSpawnPosition;
        spawnPosition += _fallbackSpawnStep * (PhotonNetwork.LocalPlayer.ActorNumber - 1);
        SpawnLocalSelectedCharacter(spawnPosition, Quaternion.identity, true);
    }

    internal GameObject SpawnLocalSelectedCharacter(Vector3 position, Quaternion rotation, bool forceRespawn)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return null;

        if (_localSpawnedPlayer != null)
        {
            if (!forceRespawn)
                return _localSpawnedPlayer;

            PhotonView view = _localSpawnedPlayer.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
                PhotonNetwork.Destroy(_localSpawnedPlayer);

            _localSpawnedPlayer = null;
        }

        string characterId = ResolveSelectedCharacterId();
        string prefabName = ResolvePlayerPrefabName(characterId);
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            Debug.LogWarning("[NetworkAuthorityManager] prefabName is empty. check character spawn entries.");
            return null;
        }

        string instantiateKey = ResolvePhotonPrefabKey(prefabName);
        if (string.IsNullOrWhiteSpace(instantiateKey))
        {
            Debug.LogWarning($"[NetworkAuthorityManager] Photon prefab not found in Resources. prefabName={prefabName}");
            return null;
        }

        try
        {
            _localSpawnedPlayer = PhotonNetwork.Instantiate(instantiateKey, position, rotation);
            Debug.Log($"[NetworkAuthorityManager] Spawned player prefab={instantiateKey}, characterId={characterId}");
            return _localSpawnedPlayer;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[NetworkAuthorityManager] Spawn failed for prefab '{instantiateKey}': {e.Message}");
            return null;
        }
    }

    private string ResolvePlayerPrefabName(string characterId)
    {
        EnsureCharacterCatalogLoaded();

        if (_characterCatalog != null)
        {
            GameObject catalogPrefab = _characterCatalog.GetPrefabOrDefault(characterId, _defaultCharacterId);
            if (catalogPrefab != null)
                return catalogPrefab.name;
        }

        if (!string.IsNullOrWhiteSpace(characterId) && _characterSpawnEntries != null)
        {
            for (int i = 0; i < _characterSpawnEntries.Count; i++)
            {
                CharacterSpawnEntry entry = _characterSpawnEntries[i];
                if (string.Equals(entry.characterId, characterId, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(entry.prefabName))
                    return entry.prefabName;
            }
        }

        if (!string.IsNullOrWhiteSpace(_defaultCharacterId) && _characterSpawnEntries != null)
        {
            for (int i = 0; i < _characterSpawnEntries.Count; i++)
            {
                CharacterSpawnEntry entry = _characterSpawnEntries[i];
                if (string.Equals(entry.characterId, _defaultCharacterId, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(entry.prefabName))
                    return entry.prefabName;
            }
        }

        string savedPrefab = PlayerPrefs.GetString(PREF_SELECTED_CHARACTER_PREFAB, string.Empty);
        if (!string.IsNullOrWhiteSpace(savedPrefab))
            return savedPrefab;

        return _defaultPlayerPrefabName;
    }

    private void EnsureCharacterCatalogLoaded()
    {
        if (_characterCatalog != null || _characterCatalogTriedLoad)
            return;

        _characterCatalogTriedLoad = true;
        _characterCatalog = Resources.Load<CharacterCatalog>(CHARACTER_CATALOG_RESOURCE_PATH);
        if (_characterCatalog == null)
            Debug.LogWarning("[NetworkAuthorityManager] CharacterCatalog not found in Resources/CharacterCatalog.");
    }

    private static string ResolvePhotonPrefabKey(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
            return string.Empty;

        if (Resources.Load<GameObject>(prefabName) != null)
            return prefabName;

        string keyInCharacters = $"Characters/{prefabName}";
        if (Resources.Load<GameObject>(keyInCharacters) != null)
            return keyInCharacters;

        string keyInPrefabsCharacters = $"Prefabs/Characters/{prefabName}";
        if (Resources.Load<GameObject>(keyInPrefabsCharacters) != null)
            return keyInPrefabsCharacters;

        return string.Empty;
    }

    private string ResolveSelectedCharacterId()
    {
        if (DataManager.Instance != null &&
            DataManager.Instance.CurrentUserData != null &&
            !string.IsNullOrWhiteSpace(DataManager.Instance.CurrentUserData.selectedCharacterId))
        {
            return DataManager.Instance.CurrentUserData.selectedCharacterId;
        }

        string savedId = PlayerPrefs.GetString(PREF_SELECTED_CHARACTER_ID, string.Empty);
        if (!string.IsNullOrWhiteSpace(savedId))
            return savedId;

        return string.Empty;
    }
}
