using UnityEngine;
using UnityEngine.Serialization;

public class CharacterData : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private string _characterId;
    [FormerlySerializedAs("_displayName")]
    [SerializeField] private string _characterName;
    [SerializeField] private string _characterExplain;
    [SerializeField] private GameObject _characterPrefab;

    public string CharacterId => _characterId;
    public string CharacterName => string.IsNullOrWhiteSpace(_characterName) ? gameObject.name : _characterName;
    public string CharacterExplain => _characterExplain;
    public GameObject CharacterPrefab => _characterPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(_characterName))
            _characterName = gameObject.name;
    }
#endif
}
