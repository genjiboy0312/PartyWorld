using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterCreationManager : MonoBehaviour
{
    private const string PREF_SELECTED_CHARACTER_INDEX = "PW_SELECTED_CHARACTER_INDEX";
    private const string PREF_SELECTED_CHARACTER_NAME = "PW_SELECTED_CHARACTER_NAME";

    [Header("UI References")]
    [SerializeField] private GameObject _characterObj;
    [SerializeField] private GameObject _checkingUI;
    [SerializeField] private ScrollRect _characterScroll;
    [SerializeField] private RectTransform _characterContent;
    [SerializeField] private Text _checkingText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    [Header("Scene")]
    [SerializeField] private string _nextSceneName = "Scene_WaitingRoom";

    [Header("UI Text")]
    [SerializeField] private string _idleText = "캐릭터를 선택해 주세요.";
    [SerializeField] private string _selectedTextFormat = "선택된 캐릭터: {0}";

    private readonly List<Button> _characterButtons = new List<Button>();
    private int _selectedIndex = -1;
    private bool _isChecking;

    private void Start()
    {
        AutoWireIfNeeded();
        BuildCharacterButtons();
        WireCheckingButtons();

        SetCheckingState(false);
        SetStatusText(_idleText);
        UpdateConfirmInteractable();
    }

    private void AutoWireIfNeeded()
    {
        if (_characterScroll == null)
        {
            GameObject scrollObj = GameObject.Find("Scroll_CharacterSelect");
            if (scrollObj != null)
                _characterScroll = scrollObj.GetComponent<ScrollRect>();
        }

        if (_characterContent == null)
        {
            if (_characterScroll != null && _characterScroll.content != null)
                _characterContent = _characterScroll.content;
            else
            {
                GameObject contentObj = GameObject.Find("Content");
                if (contentObj != null)
                    _characterContent = contentObj.GetComponent<RectTransform>();
            }
        }

        if (_checkingUI == null)
            _checkingUI = GameObject.Find("UI_Checking");

        if (_checkingText == null)
        {
            GameObject textObj = _checkingUI != null
                ? FindChildByName(_checkingUI.transform, "Text_Checking")
                : GameObject.Find("Text_Checking");

            if (textObj != null)
                _checkingText = textObj.GetComponent<Text>();
        }

        if (_confirmButton == null && _checkingUI != null)
        {
            GameObject btnObj = FindChildByName(_checkingUI.transform, "Btn_Confirm");
            if (btnObj != null)
                _confirmButton = btnObj.GetComponent<Button>();
        }

        if (_cancelButton == null && _checkingUI != null)
        {
            GameObject btnObj = FindChildByName(_checkingUI.transform, "Btn_Cancel");
            if (btnObj != null)
                _cancelButton = btnObj.GetComponent<Button>();
        }

        if (_characterObj == null)
        {
            GameObject candidate = GameObject.Find("Character");
            if (candidate == null)
                candidate = GameObject.Find("Player");
            if (candidate == null)
                candidate = GameObject.Find("CharacterPreview");

            _characterObj = candidate;
        }
    }

    private static GameObject FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child.gameObject;

            GameObject recursive = FindChildByName(child, childName);
            if (recursive != null)
                return recursive;
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        AutoWireIfNeeded();
    }

    [ContextMenu("Auto Wire References")]
    private void AutoWireFromContextMenu()
    {
        AutoWireIfNeeded();
    }
#endif

    private void BuildCharacterButtons()
    {
        _characterButtons.Clear();

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

            int capturedIndex = _characterButtons.Count;
            button.onClick.AddListener(() => OnCharacterClicked(capturedIndex));
            _characterButtons.Add(button);
        }
    }

    private void WireCheckingButtons()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(OnConfirmClicked);

        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnCharacterClicked(int index)
    {
        if (index < 0 || index >= _characterButtons.Count)
            return;

        _selectedIndex = index;
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

        string selectedName = GetSelectedCharacterName();
        PlayerPrefs.SetInt(PREF_SELECTED_CHARACTER_INDEX, _selectedIndex);
        PlayerPrefs.SetString(PREF_SELECTED_CHARACTER_NAME, selectedName);
        PlayerPrefs.Save();

        SceneManager.LoadScene(_nextSceneName);
    }

    private void OnCancelClicked()
    {
        SetCheckingState(false);
    }

    private string GetSelectedCharacterName()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _characterButtons.Count)
            return "None";

        return _characterButtons[_selectedIndex].gameObject.name;
    }

    private void UpdateConfirmInteractable()
    {
        if (_confirmButton != null)
            _confirmButton.interactable = _selectedIndex >= 0;
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
}
