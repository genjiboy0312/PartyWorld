using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterCreationManager : MonoBehaviour
{
    private const string PREF_SELECTED_CHARACTER_INDEX = "PW_SELECTED_CHARACTER_INDEX";
    private const string PREF_SELECTED_CHARACTER_NAME = "PW_SELECTED_CHARACTER_NAME";
    private const string PREF_SELECTED_CHARACTER_ID = "PW_SELECTED_CHARACTER_ID";
    private const string PREF_SELECTED_CHARACTER_PREFAB = "PW_SELECTED_CHARACTER_PREFAB";

    [Header("UI References")]
    [SerializeField] private GameObject _checkingUI;
    [SerializeField] private ScrollRect _characterScroll;
    [SerializeField] private RectTransform _characterContent;
    [SerializeField] private Transform _posCharacter;
    [SerializeField] private Button _characterSelectButton;
    [Header("UI Checking")]
    [SerializeField] private Text _checkingText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Header("Character Data")]
    [SerializeField] private List<CharacterData> _characterDatas;

    [Header("Scene")]
    [SerializeField] private string _nextSceneName = "Scene_WaitingRoom";

    [Header("UI Text")]
    [SerializeField] private string _idleText = "캐릭터를 선택해 주세요.";
    [SerializeField] private string _selectedTextFormat = "선택된 캐릭터: {0}";

    [Header("UI Character Info")]
    [SerializeField] private Text _characterName;
    [SerializeField] private Text _characterExplain;
    [SerializeField] private string _defaultCharacterName = "";
    [SerializeField] private string _defaultCharacterExplain = "";

    private readonly List<Button> _characterButtons = new List<Button>();
    private readonly List<CharacterData> _buttonCharacterDatas = new List<CharacterData>();
    private int _selectedIndex = -1;
    private bool _isChecking;
    private CharacterData _selectedCharacterData;
    private GameObject _previewCharacterInstance;

    private void Start()
    {
        ValidateReferences();
        SyncCharacterDataFromContentIfNeeded();
        BuildCharacterButtons();
        WireCheckingButtons();

        SetCheckingState(false);
        SetStatusText(_idleText);
        UpdateCharacterInfoUI();
        UpdateCharacterSelectInteractable();
        UpdateConfirmInteractable();
    }

    private void ValidateReferences()
    {
        if (_characterScroll != null && _characterContent == null)
            _characterContent = _characterScroll.content;

        if (_characterContent == null)
            Debug.LogWarning("[CharacterCreationManager] _characterContent is not assigned.", this);

        if (_checkingUI == null)
            Debug.LogWarning("[CharacterCreationManager] _checkingUI is not assigned.", this);

        if (_checkingText == null)
            Debug.LogWarning("[CharacterCreationManager] _checkingText is not assigned.", this);

        if (_posCharacter == null)
            Debug.LogWarning("[CharacterCreationManager] _posCharacter is not assigned.", this);

        if (_characterSelectButton == null)
            Debug.LogWarning("[CharacterCreationManager] _characterSelectButton is not assigned.", this);

        if (_confirmButton == null)
            Debug.LogWarning("[CharacterCreationManager] _confirmButton is not assigned.", this);

        if (_cancelButton == null)
            Debug.LogWarning("[CharacterCreationManager] _cancelButton is not assigned.", this);

        if (_characterName == null)
            Debug.LogWarning("[CharacterCreationManager] _characterName is not assigned.", this);

        if (_characterExplain == null)
            Debug.LogWarning("[CharacterCreationManager] _characterExplain is not assigned.", this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_characterScroll != null && _characterContent == null)
            _characterContent = _characterScroll.content;
    }
#endif

    private void BuildCharacterButtons()
    {
        _characterButtons.Clear();
        _buttonCharacterDatas.Clear();

        if (_characterContent == null)
            return;

        for (int i = 0; i < _characterContent.childCount; i++)
        {
            Transform child = _characterContent.GetChild(i);
            if (child == null)
                continue;

            Button button = child.GetComponent<Button>();
            if (button == null)
                continue;

            CharacterData data = child.GetComponent<CharacterData>();
            if (data == null)
                continue;

            int dataIndex = _characterDatas.IndexOf(data);
            if (dataIndex < 0)
                continue;

            int capturedIndex = _characterButtons.Count;
            button.onClick.AddListener(() => OnCharacterClicked(capturedIndex));
            _characterButtons.Add(button);
            _buttonCharacterDatas.Add(_characterDatas[dataIndex]);
        }
    }

    private void SyncCharacterDataFromContentIfNeeded()
    {
        if (_characterDatas == null)
            _characterDatas = new List<CharacterData>();

        if (_characterContent == null)
            return;

        if (_characterDatas.Count > 0)
            return;

        for (int i = 0; i < _characterContent.childCount; i++)
        {
            Transform child = _characterContent.GetChild(i);
            if (child == null)
                continue;

            CharacterData data = child.GetComponent<CharacterData>();
            if (data != null)
                _characterDatas.Add(data);
        }
    }

    private void WireCheckingButtons()
    {
        if (_characterSelectButton != null)
            _characterSelectButton.onClick.AddListener(OnCharacterSelectButtonClicked);

        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnCharacterClicked(int index)
    {
        if (index < 0 || index >= _buttonCharacterDatas.Count)
            return;

        _selectedIndex = index;
        _selectedCharacterData = _buttonCharacterDatas[index];
        ShowSelectedCharacterOnPos();
        SetStatusText(string.Format(_selectedTextFormat, GetSelectedCharacterName()));
        UpdateCharacterInfoUI();
        UpdateCharacterSelectInteractable();
        UpdateConfirmInteractable();
    }

    private void OnCharacterSelectButtonClicked()
    {
        if (_selectedCharacterData == null)
        {
            SetStatusText(_idleText);
            Debug.LogWarning("[CharacterCreationManager] No character selected.", this);
            return;
        }

        SetCheckingState(true);
        SetStatusText(string.Format(_selectedTextFormat, GetSelectedCharacterName()));
        UpdateConfirmInteractable();
    }

    private void OnConfirmClicked()
    {
        if (_selectedIndex < 0)
            return;

        if (string.IsNullOrWhiteSpace(_nextSceneName))
            return;

        if (_selectedCharacterData == null)
            return;

        string selectedId = _selectedCharacterData.CharacterId;
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            Debug.LogWarning("[CharacterCreationManager] CharacterId is empty. Set CharacterData._characterId before confirming.", this);
            return;
        }

        string selectedName = GetSelectedCharacterName();
        string selectedPrefabName = _selectedCharacterData.CharacterPrefab != null
            ? _selectedCharacterData.CharacterPrefab.name
            : string.Empty;

        PlayerPrefs.SetInt(PREF_SELECTED_CHARACTER_INDEX, _selectedIndex);
        PlayerPrefs.SetString(PREF_SELECTED_CHARACTER_NAME, selectedName);
        PlayerPrefs.SetString(PREF_SELECTED_CHARACTER_ID, selectedId);
        PlayerPrefs.SetString(PREF_SELECTED_CHARACTER_PREFAB, selectedPrefabName);
        PlayerPrefs.Save();

        SceneManager.LoadScene(_nextSceneName);
    }

    private void OnCancelClicked()
    {
        SetCheckingState(false);
    }

    private string GetSelectedCharacterName()
    {
        if (_selectedCharacterData != null)
            return _selectedCharacterData.CharacterName;

        if (_selectedIndex < 0 || _selectedIndex >= _characterButtons.Count)
            return "None";

        return _characterButtons[_selectedIndex].gameObject.name;
    }

    private void UpdateCharacterInfoUI()
    {
        if (_selectedCharacterData == null)
        {
            if (_characterName != null)
                _characterName.text = _defaultCharacterName;

            if (_characterExplain != null)
                _characterExplain.text = _defaultCharacterExplain;

            return;
        }

        if (_characterName != null)
            _characterName.text = _selectedCharacterData.CharacterName;

        if (_characterExplain != null)
            _characterExplain.text = _selectedCharacterData.CharacterExplain;
    }

    private void UpdateConfirmInteractable()
    {
        if (_confirmButton != null)
            _confirmButton.interactable = _selectedIndex >= 0;
    }

    private void UpdateCharacterSelectInteractable()
    {
        if (_characterSelectButton != null)
            _characterSelectButton.interactable = _selectedIndex >= 0;
    }

    private void SetStatusText(string message)
    {
        if (_checkingText != null)
            _checkingText.text = message;
    }

    private void SetCheckingState(bool isOn)
    {
        _isChecking = isOn;
        if (_checkingUI != null)
            _checkingUI.SetActive(_isChecking);
    }

    private void ShowSelectedCharacterOnPos()
    {
        if (_posCharacter == null)
            return;

        if (_previewCharacterInstance != null)
            Destroy(_previewCharacterInstance);

        if (_selectedCharacterData == null || _selectedCharacterData.CharacterPrefab == null)
            return;

        _previewCharacterInstance = Instantiate(_selectedCharacterData.CharacterPrefab, _posCharacter);
        _previewCharacterInstance.transform.localPosition = Vector3.zero;
        _previewCharacterInstance.transform.localRotation = Quaternion.identity;
        _previewCharacterInstance.transform.localScale = Vector3.one;
    }

}
