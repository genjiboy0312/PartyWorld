using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

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

        GameObject go = new GameObject(nameof(GameManager));
        go.AddComponent<GameManager>();

        // 플랫폼 품질 관리자 초기화 (GameManager와 동일한 GameObject에 추가)
        if (PlatformQualityManager.Instance == null)
            go.AddComponent<PlatformQualityManager>();
    }

    [SerializeField] private GameState _currentGameState = GameState.Title;
    [SerializeField] private static int _stage;

    [Header("Scene-State Mapping")]
    [SerializeField] private SceneReference _titleScene = new SceneReference();
    [SerializeField] private SceneReference _characterCreationScene = new SceneReference();
    [SerializeField] private SceneReference _waitingRoomScene = new SceneReference();
    [SerializeField] private SceneReference _lobbyScene = new SceneReference();
    [SerializeField] private SceneReference _loadingScene = new SceneReference();
    [SerializeField] private SceneReference _resultScene = new SceneReference();
    [SerializeField] private List<SceneReference> _mapScenes = new List<SceneReference>();

    // 런타임용 문자열 캐시
    private string _titleSceneName;
    private string _characterCreationSceneName;
    private string _waitingRoomSceneName;
    private string _lobbySceneName;
    private string _loadingSceneName;
    private string _resultSceneName;
    private HashSet<string> _mapSceneNames = new HashSet<string>();

    [SerializeField] private string _mapScenePrefix = "Scene_Map";

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

        // SceneReference에서 문자열 캐시 생성
        _titleSceneName = _titleScene?.SceneName ?? "";
        _characterCreationSceneName = _characterCreationScene?.SceneName ?? "";
        _waitingRoomSceneName = _waitingRoomScene?.SceneName ?? "";
        _lobbySceneName = _lobbyScene?.SceneName ?? "";
        _loadingSceneName = _loadingScene?.SceneName ?? "";
        _resultSceneName = _resultScene?.SceneName ?? "";

        // 맵 씬 HashSet 구성
        _mapSceneNames.Clear();
        if (_mapScenes != null)
        {
            foreach (var scene in _mapScenes)
            {
                string name = scene?.SceneName;
                if (!string.IsNullOrWhiteSpace(name))
                    _mapSceneNames.Add(name);
            }
        }

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

    // ──────────────────────────────────────────────
    // Round System Integration
    // ──────────────────────────────────────────────
    /// RoundManager가 준비되었음을 알림 (마스터 클라이언트가 로드 완료 집계 후 호출)
    public static void NotifyRoundContainerReady() { }

    /// 모든 라운드가 종료되면 Result 씬으로 전환
    public void OnRoundSessionComplete()
    {
        SetGameState(GameState.Result);
        PhotonNetwork.LoadLevel(_resultSceneName);
    }

    public void InitializeChatManager()
    {
        // ChatManager는 자신의 Awake에서 GameManager.Instance를 통해 직접 구독하므로
        // GameManager는 중복 구독 방지를 위해 참조 정리만 수행
        if (_chatMgr != null)
        {
            _onGameStateChange -= _chatMgr.OnGameStateChange;
            _chatMgr = null;
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
        if (_mapSceneNames == null || string.IsNullOrWhiteSpace(sceneName))
            return false;

        return _mapSceneNames.Contains(sceneName);
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
