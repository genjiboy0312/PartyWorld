using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Title,
    Loading,
    Playing,
    GameOver,
    CharacterCreation,
    WaitingRoom,
    Lobby,
    Result
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // 씬에 배치되지 않아도 GameManager 인스턴스를 1개 보장
        if (Instance != null)
            return;

        if (FindAnyObjectByType<GameManager>() != null)
            return;

        GameObject go = new GameObject(nameof(GameManager));
        go.AddComponent<GameManager>();
    }

    [SerializeField] private GameState _currentGameState = GameState.Title;
    [SerializeField] private static int _stage;

    [Header("Scene-State Mapping")]
    [SerializeField] private string _titleSceneName = "Scene_Title&Login";
    [SerializeField] private string _characterCreationSceneName = "Scene_CharacterCreation";
    [SerializeField] private string _waitingRoomSceneName = "Scene_WaitingRoom";
    [SerializeField] private string _lobbySceneName = "Scene_Lobby";
    [SerializeField] private string _loadingSceneName = "Scene_Loading";
    [SerializeField] private string _resultSceneName = "";
    [SerializeField] private string _mapScenePrefix = "Scene_Map";

    // 옵저버 패턴
    private event Action<GameState> _onGameStateChange;

    public event Action<GameState> OnGameStateChangeEvent
    {
        add => _onGameStateChange += value;
        remove => _onGameStateChange -= value;
    }

    private ChatManager _chatMgr;

    public GameState CurrentGameState => _currentGameState;
    public static int Stage
    {
        get => _stage;
        set => _stage = value;
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

        _stage = 1;

        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeChatManager();
        SyncStateByScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_chatMgr != null)
        {
            _onGameStateChange -= _chatMgr.OnGameStateChange;
            _chatMgr = null;
        }

        // 싱글톤 정리
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeChatManager();
        SyncStateByScene(scene.name);
    }

    public void StartGame() => SetGameState(GameState.Playing);
    public void GameOver() => SetGameState(GameState.GameOver);

    public void InitializeChatManager()
    {
        if (_chatMgr != null)
        {
            _onGameStateChange -= _chatMgr.OnGameStateChange;
            _chatMgr = null;
        }

        _chatMgr = FindAnyObjectByType<ChatManager>();

        if (_chatMgr != null)
        {
            _onGameStateChange += _chatMgr.OnGameStateChange;
        }
    }

    public void SetGameState(GameState newGameState)
    {
        // 중복 상태 변경 방지
        if (_currentGameState == newGameState)
            return;

        _currentGameState = newGameState;

        // 옵저버 이벤트 안전 호출
        SafeInvokeGameStateChange(newGameState);
    }

    public void SyncStateByScene(string sceneName)
    {
        if (!TryResolveGameState(sceneName, out GameState resolved))
            return;

        SetGameState(resolved);
    }

    private bool TryResolveGameState(string sceneName, out GameState state)
    {
        state = _currentGameState;

        if (string.IsNullOrWhiteSpace(sceneName))
            return false;

        if (IsScene(sceneName, _titleSceneName))
        {
            state = GameState.Title;
            return true;
        }

        if (IsScene(sceneName, _characterCreationSceneName))
        {
            state = GameState.CharacterCreation;
            return true;
        }

        if (IsScene(sceneName, _waitingRoomSceneName))
        {
            state = GameState.WaitingRoom;
            return true;
        }

        if (IsScene(sceneName, _lobbySceneName))
        {
            state = GameState.Lobby;
            return true;
        }

        if (IsScene(sceneName, _loadingSceneName))
        {
            state = GameState.Loading;
            return true;
        }

        if (IsScene(sceneName, _resultSceneName))
        {
            state = GameState.Result;
            return true;
        }

        if (IsMapScene(sceneName))
        {
            state = GameState.Playing;
            return true;
        }

        return false;
    }

    private bool IsMapScene(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(_mapScenePrefix) &&
            sceneName.StartsWith(_mapScenePrefix, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool IsScene(string sceneName, string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
            return false;

        return string.Equals(sceneName, configured, StringComparison.OrdinalIgnoreCase);
    }

    // 개선된 안전한 이벤트 호출
    private void SafeInvokeGameStateChange(GameState newGameState)
    {
        if (_onGameStateChange == null)
            return;

        // 각 구독자를 안전하게 호출
        foreach (Delegate subscriber in _onGameStateChange.GetInvocationList())
        {
            try
            {
                var action = subscriber as Action<GameState>;
                action?.Invoke(newGameState);
            }
            catch (Exception e)
            {
                Debug.LogError($"GameState 이벤트 호출 오류 [{subscriber.Method.Name}]: {e.Message}");
            }
        }
    }
}
