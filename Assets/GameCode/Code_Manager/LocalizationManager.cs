using UnityEngine;
using System;

/// <summary>
/// 언어 설정을 관리하는 매니저 (싱글톤)
/// 0: 한국어, 1: English
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("Language Settings")]
    [SerializeField] private int _currentLanguageIndex = 0;

    private const string PlayerPrefsKey = "SelectedLanguage";

    /// <summary>언어 변경 시 발생하는 이벤트 (int: language index)</summary>
    public Action<int> OnLanguageChanged;

    public int CurrentLanguageIndex => _currentLanguageIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject(nameof(LocalizationManager));
        go.AddComponent<LocalizationManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _currentLanguageIndex = PlayerPrefs.GetInt(PlayerPrefsKey, 0);
    }

    /// <summary>
    /// 언어를 변경하고 PlayerPrefs에 저장한다.
    /// </summary>
    /// <param name="index">0=한국어, 1=English</param>
    public void SetLanguage(int index)
    {
        if (index < 0 || index > 1)
        {
            Debug.LogWarning($"[LocalizationManager] Invalid language index: {index}");
            return;
        }

        if (_currentLanguageIndex == index) return;

        _currentLanguageIndex = index;
        PlayerPrefs.SetInt(PlayerPrefsKey, index);
        PlayerPrefs.Save();

        Debug.Log($"[LocalizationManager] Language changed to index {index}");
        OnLanguageChanged?.Invoke(index);
    }
}
