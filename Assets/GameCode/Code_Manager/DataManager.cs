using UnityEngine;
using System;

#if FIREBASE_DATABASE
using Firebase.Database;
#endif

// 유저 데이터를 전역적으로 관리하는 싱글톤 매니저
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

#if FIREBASE_DATABASE
    private const string RealtimeDatabaseUrl = "https://project-playworld-default-rtdb.firebaseio.com/";
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
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

    public void SaveData()
    {
        SaveUserDataToFirebase();
    }

    public bool TryGetUserProfilePath(string userId, out string profilePath)
    {
        profilePath = FirebaseDbPaths.UserProfile(userId);
        return !string.IsNullOrEmpty(profilePath);
    }

    public bool TryGetCurrentUserProfilePath(out string profilePath)
    {
        string userId = _currentUserData != null ? _currentUserData.userId : string.Empty;
        return TryGetUserProfilePath(userId, out profilePath);
    }

    public void MarkLoginNow()
    {
        if (_currentUserData == null)
            _currentUserData = new UserData();

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _currentUserData.lastLoginAt = now;

        if (_currentUserData.createdAt <= 0)
            _currentUserData.createdAt = now;

        if (_currentUserData.updatedAt <= 0)
            _currentUserData.updatedAt = now;
    }

    public bool SaveUserData()
    {
        if (!TryBuildProfileSavePayload(out string profilePath, out string json))
            return false;

        Debug.Log($"[DataManager] 데이터 저장 시도 Path={profilePath}, JSON={json}");
        return true;
    }

    private bool TryBuildProfileSavePayload(out string profilePath, out string json)
    {
        profilePath = string.Empty;
        json = string.Empty;

        if (_currentUserData == null)
        {
            Debug.LogWarning("[DataManager] CurrentUserData가 비어 있습니다.");
            return false;
        }

        if (!TryGetCurrentUserProfilePath(out profilePath))
        {
            Debug.LogWarning("[DataManager] userId가 비어 있어 DB 경로를 만들 수 없습니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_currentUserData.nickname) || _currentUserData.nickname == "NewPlayer")
        {
            Debug.LogWarning("[DataManager] nickname이 유효하지 않아 저장을 건너뜁니다.");
            return false;
        }

        _currentUserData.updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        json = _currentUserData.ToJson();
        return true;
    }

    public async void SaveUserDataToFirebase(Action<bool> onCompleted = null)
    {
        if (!TryBuildProfileSavePayload(out string profilePath, out string json))
        {
            onCompleted?.Invoke(false);
            return;
        }

#if FIREBASE_DATABASE
        try
        {
            DatabaseReference db = GetDatabaseReference(profilePath);
            await db.SetRawJsonValueAsync(json);
            Debug.Log($"[DataManager] Firebase 저장 완료: {profilePath}");
            onCompleted?.Invoke(true);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DataManager] Firebase 저장 실패: {e.Message}");
            onCompleted?.Invoke(false);
        }
#else
        Debug.LogWarning("[DataManager] Firebase Database SDK가 없어 실제 저장은 실행되지 않았습니다. (FIREBASE_DATABASE)");
        Debug.Log($"[DataManager] Mock Save Path={profilePath}, JSON={json}");
        onCompleted?.Invoke(false);
#endif
    }

    public async void LoadUserDataFromFirebase(string userId, Action<bool> onCompleted = null)
    {
        if (!TryGetUserProfilePath(userId, out string profilePath))
        {
            Debug.LogWarning("[DataManager] userId가 비어 있어 Firebase 로드를 건너뜁니다.");
            onCompleted?.Invoke(false);
            return;
        }

#if FIREBASE_DATABASE
        try
        {
            DatabaseReference db = GetDatabaseReference(profilePath);
            DataSnapshot snapshot = await db.GetValueAsync();

            if (snapshot == null || !snapshot.Exists)
            {
                _currentUserData = new UserData();
                _currentUserData.userId = userId;
                MarkLoginNow();
                Debug.LogWarning($"[DataManager] Firebase 데이터 없음. 기본 UserData 생성: {profilePath}");
                onCompleted?.Invoke(false);
                return;
            }

            string json = snapshot.GetRawJsonValue();
            bool loaded = LoadUserDataFromJson(json);

            if (_currentUserData == null)
                _currentUserData = new UserData();

            _currentUserData.userId = userId;
            MarkLoginNow();

            Debug.Log($"[DataManager] Firebase 로드 완료: {profilePath}");
            onCompleted?.Invoke(loaded);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DataManager] Firebase 로드 실패: {e.Message}");
            onCompleted?.Invoke(false);
        }
#else
        Debug.LogWarning("[DataManager] Firebase Database SDK가 없어 실제 로드는 실행되지 않았습니다. (FIREBASE_DATABASE)");
        Debug.Log($"[DataManager] Mock Load Path={profilePath}");
        onCompleted?.Invoke(false);
#endif
    }

#if FIREBASE_DATABASE
    private static DatabaseReference GetDatabaseReference(string path)
    {
        FirebaseDatabase database = FirebaseDatabase.GetInstance(RealtimeDatabaseUrl);
        return database.GetReference(path);
    }
#endif

    public bool LoadUserDataFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[DataManager] 로드할 데이터가 없습니다. 기본 데이터를 생성합니다.");
            _currentUserData = new UserData();
            return false;
        }

        _currentUserData = UserData.FromJson(json);
        Debug.Log($"[DataManager] 데이터 로드 완료: {_currentUserData.nickname}");
        return true;
    }

    public void LoadData(string json)
    {
        LoadUserDataFromJson(json);
    }
}
