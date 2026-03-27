using System;
using System.Collections.Generic;
using UnityEngine;

public class WaitingRoomCharacterPreview : MonoBehaviour
{
    [Serializable]
    private struct CharacterPreviewEntry
    {
        public string characterId;
        public GameObject prefab;
    }

    private const string PrefSelectedCharacterId = "PW_SELECTED_CHARACTER_ID";
    private const string CharacterCatalogResourcePath = "CharacterCatalog";

    [SerializeField] private Transform _playerPreviewRoot;
    [SerializeField] private CharacterCatalog _characterCatalog;
    [SerializeField] private List<CharacterPreviewEntry> _entries = new List<CharacterPreviewEntry>();
    [SerializeField] private string _defaultCharacterId = "bear_base";
    [SerializeField] private bool _lockLocalTransformToZero = true;

    private GameObject _previewInstance;
    private bool _catalogTriedLoad;

    private void Start()
    {
        string characterId = ResolveSelectedCharacterId();
        SpawnPreview(characterId);
    }

    private void LateUpdate()
    {
        if (!_lockLocalTransformToZero || _previewInstance == null)
            return;

        _previewInstance.transform.localPosition = Vector3.zero;
        _previewInstance.transform.localEulerAngles = Vector3.zero;
    }

    private string ResolveSelectedCharacterId()
    {
        if (DataManager.Instance != null &&
            DataManager.Instance.CurrentUserData != null &&
            !string.IsNullOrWhiteSpace(DataManager.Instance.CurrentUserData.selectedCharacterId))
        {
            return DataManager.Instance.CurrentUserData.selectedCharacterId;
        }

        return PlayerPrefs.GetString(PrefSelectedCharacterId, string.Empty);
    }

    private void SpawnPreview(string characterId)
    {
        if (_playerPreviewRoot == null)
        {
            Debug.LogWarning("[WaitingRoomCharacterPreview] _playerPreviewRoot is not assigned.", this);
            return;
        }

        if (_previewInstance != null)
            Destroy(_previewInstance);

        GameObject prefab = FindPrefab(characterId);
        if (prefab == null)
        {
            Debug.LogWarning($"[WaitingRoomCharacterPreview] Preview prefab not found. characterId={characterId}", this);
            return;
        }

        _previewInstance = Instantiate(prefab, _playerPreviewRoot);
        _previewInstance.transform.localPosition = Vector3.zero;
        _previewInstance.transform.localRotation = Quaternion.identity;
        _previewInstance.transform.localEulerAngles = Vector3.zero;
        _previewInstance.transform.localScale = Vector3.one;

        Rigidbody[] rigidbodies = _previewInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].useGravity = false;
            rigidbodies[i].isKinematic = true;
        }
    }

    private GameObject FindPrefab(string characterId)
    {
        EnsureCatalogLoaded();

        if (_characterCatalog != null)
        {
            GameObject fromCatalog = _characterCatalog.GetPrefabOrDefault(characterId, _defaultCharacterId);
            if (fromCatalog != null)
                return fromCatalog;
        }

        if (!string.IsNullOrWhiteSpace(characterId))
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].characterId, characterId, StringComparison.OrdinalIgnoreCase) &&
                    _entries[i].prefab != null)
                {
                    return _entries[i].prefab;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(_defaultCharacterId))
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].characterId, _defaultCharacterId, StringComparison.OrdinalIgnoreCase) &&
                    _entries[i].prefab != null)
                {
                    return _entries[i].prefab;
                }
            }
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].prefab != null)
                return _entries[i].prefab;
        }

        return null;
    }

    private void EnsureCatalogLoaded()
    {
        if (_characterCatalog != null || _catalogTriedLoad)
            return;

        _catalogTriedLoad = true;
        _characterCatalog = Resources.Load<CharacterCatalog>(CharacterCatalogResourcePath);
    }
}
