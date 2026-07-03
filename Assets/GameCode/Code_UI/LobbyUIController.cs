using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text _txtPlayerId;
    [SerializeField] private Dropdown _dropdownLanguage;

    private void Start()
    {
        // Txt_playerId에 플레이어 닉네임 표시
        if (_txtPlayerId != null)
        {
            string nick = DataManager.Instance != null
                ? DataManager.Instance.CurrentUserData.nickname
                : string.Empty;
            if (string.IsNullOrWhiteSpace(nick))
                nick = "Player";
            _txtPlayerId.text = nick;
        }

        // Dropdown_Language 초기화 및 리스너 연결
        if (_dropdownLanguage != null)
        {
            _dropdownLanguage.ClearOptions();
            _dropdownLanguage.options.Add(new Dropdown.OptionData("Korea"));
            _dropdownLanguage.options.Add(new Dropdown.OptionData("English"));

            if (LocalizationManager.Instance != null)
                _dropdownLanguage.value = LocalizationManager.Instance.CurrentLanguageIndex;

            _dropdownLanguage.onValueChanged.AddListener(OnLanguageChanged);
        }
    }

    private void OnDestroy()
    {
        if (_dropdownLanguage != null)
            _dropdownLanguage.onValueChanged.RemoveListener(OnLanguageChanged);
    }

    private void OnLanguageChanged(int index)
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.SetLanguage(index);
    }
}
