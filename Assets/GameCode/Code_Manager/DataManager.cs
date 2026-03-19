using UnityEngine;

// 유저 데이터를 전역적으로 관리하는 싱글톤 매니저
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // 씬에 배치되지 않아도 인스턴스를 1개 보장
        if (Instance != null)
            return;

        if (FindAnyObjectByType<DataManager>() != null)
            return;

        GameObject go = new GameObject(nameof(DataManager));
        go.AddComponent<DataManager>();
    }

    [Header("User Data")]
    [SerializeField] private UserData _currentUserData = new UserData();

    public UserData CurrentUserData
    {
        get => _currentUserData;
        set => _currentUserData = value;
    }

    private void Awake()
    {
        // 싱글톤 처리
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void OnDestroy()
    {
        // 싱글톤 정리
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // --- 데이터 관리 기능 ---

    // 유저 데이터 초기화 (로그아웃 시 등)
    public void ClearUserData()
    {
        _currentUserData = new UserData();
    }

    // 유저 데이터 저장 (추후 Firebase 연동)
    public void SaveData()
    {
        string json = _currentUserData.ToJson();
        Debug.Log($"[DataManager] 데이터 저장 시도 (JSON): {json}");

        // TODO: Firebase Realtime Database 또는 Firestore에 저장 로직 추가
    }

    // 유저 데이터 로드 (추후 Firebase 연동)
    public void LoadData(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[DataManager] 로드할 데이터가 없습니다. 기본 데이터를 생성합니다.");
            _currentUserData = new UserData();
            return;
        }

        _currentUserData = UserData.FromJson(json);
        Debug.Log($"[DataManager] 데이터 로드 완료: {_currentUserData.nickname}");
    }
}
