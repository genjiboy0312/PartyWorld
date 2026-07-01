using UnityEngine;
using UnityEngine.UI;

public class UIGameSetting : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private Button _tabPlayer;
    [SerializeField] private Button _tabSound;
    [SerializeField] private Button _tabGraphic;

    [Header("Content Panels")]
    [SerializeField] private GameObject _contentPlayer;
    [SerializeField] private GameObject _contentSound;
    [SerializeField] private GameObject _contentGraphic;

    [Header("Graphic Settings")]
    [SerializeField] private Dropdown _qualityDropdown;

    [Header("Setting Board")]
    [SerializeField] private GameObject _settingBoard;
    [SerializeField] private Button _exitBtn;

    private void Start()
    {
        // Tab listeners
        _tabPlayer.onClick.AddListener(() => ShowTab(0));
        _tabSound.onClick.AddListener(() => ShowTab(1));
        _tabGraphic.onClick.AddListener(() => ShowTab(2));

        // Exit
        if (_exitBtn != null)
            _exitBtn.onClick.AddListener(() => _settingBoard.SetActive(false));

        // Quality dropdown
        if (_qualityDropdown != null)
        {
            _qualityDropdown.ClearOptions();
            _qualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Low", "Medium", "High" });
            _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

            // Sync with current quality
            int currentLevel = QualitySettings.GetQualityLevel();
            int currentTier = Mathf.Clamp(currentLevel, 0, 2);
            _qualityDropdown.value = currentTier;
            _qualityDropdown.RefreshShownValue();
        }

        // Default to Player tab
        ShowTab(0);
    }

    private void ShowTab(int index)
    {
        _contentPlayer.SetActive(index == 0);
        _contentSound.SetActive(index == 1);
        _contentGraphic.SetActive(index == 2);
    }

    private void OnQualityChanged(int index)
    {
        DeviceTier tier = (DeviceTier)index;

        if (PlatformQualityManager.Instance != null)
        {
            PlatformQualityManager.Instance.SetQualityOverride(tier);
        }
        else
        {
            QualitySettings.SetQualityLevel(index, true);
        }

        Debug.Log($"[UIGameSetting] Quality changed to {tier}");
    }
}
