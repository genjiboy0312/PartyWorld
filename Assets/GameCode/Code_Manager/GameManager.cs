using System;
using UnityEngine;

public enum GameState
{
    Title,
    Loading,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState _currentGameState = GameState.Title;
    [SerializeField] private static int _stage;

    // 옵저버 패턴
    private event Action<GameState> _onGameStateChange;

    public event Action<GameState> OnGameStateChangeEvent
    {
        add => _onGameStateChange += value;
        remove => _onGameStateChange -= value;
    }

    private PhotonManager _photonMgr;
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

        // PhotonManager 참조
        _photonMgr = FindObjectOfType<PhotonManager>();
        if (_photonMgr == null)
            Debug.LogError("PhotonManager를 찾을 수 없습니다!");

        InitializeChatManager();
    }

    private void Start()
    {
        StartGame();
    }

    private void OnDestroy()
    {
        // 싱글톤 정리
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartGame() => SetGameState(GameState.Playing);
    public void GameOver() => SetGameState(GameState.GameOver);

    public void InitializeChatManager()
    {
        _chatMgr = FindObjectOfType<ChatManager>();

        if (_chatMgr != null)
        {
            _onGameStateChange += _chatMgr.OnGameStateChange;
        }
        else
        {
            Debug.LogWarning("ChatManager를 찾을 수 없습니다.");
        }
    }

    private void SetGameState(GameState newGameState)
    {
        // 중복 상태 변경 방지
        if (_currentGameState == newGameState)
            return;

        // PhotonManager 호출
        if (newGameState == GameState.Playing)
            _photonMgr?.StartGame();
        else if (newGameState == GameState.GameOver)
            _photonMgr?.EndGame();

        _currentGameState = newGameState;

        // 옵저버 이벤트 안전 호출
        SafeInvokeGameStateChange(newGameState);
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