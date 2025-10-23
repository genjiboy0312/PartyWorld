using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Menu,
    Loading,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;  // 외부에서 접근 가능

    [SerializeField] private GameState _currentGameState = GameState.Menu;
    [SerializeField] private static int _stage;  // 전역 게임 스테이지

    // 옵저버 패턴: 구독자가 게임 상태 변화를 받음
    public event Action<GameState> _onGameStateChange;
    public event Action<GameState> OnGameStateChangeEvent
    {
        add { _onGameStateChange += value; }
        remove { _onGameStateChange -= value; }
    }

    private PhotonManager _photonMgr;
    private ChatManager _chatMgr;
    [SerializeField] private bool _isChatMgrCheck = false;

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

    public void StartGame()
    {
        SetGameState(GameState.Playing);
    }

    public void GameOver()
    {
        SetGameState(GameState.GameOver);
    }

    public void InitializeChatManager()
    {
        _chatMgr = FindObjectOfType<ChatManager>();
        _isChatMgrCheck = _chatMgr != null;

        if (_isChatMgrCheck)
            _onGameStateChange += _chatMgr.OnGameStateChange;
        else
            Debug.LogWarning("*** ChatManager is Null ***");
    }

    private void SetGameState(GameState newGameState)
    {
        // PhotonManager 호출
        if (newGameState == GameState.Playing)
            _photonMgr?.StartGame();
        else if (newGameState == GameState.GameOver)
            _photonMgr?.EndGame();

        _currentGameState = newGameState;

        // 옵저버 이벤트 안전 호출
        SafeInvokeGameStateChange(newGameState);
    }

    // 옵저버 이벤트 호출 시 오류 캐치
    private void SafeInvokeGameStateChange(GameState newGameState)
    {
        if (_onGameStateChange == null) return;

        foreach (Delegate d in _onGameStateChange.GetInvocationList())
        {
            try
            {
                ((Action<GameState>)d)?.Invoke(newGameState);
            }
            catch (Exception e)
            {
                Debug.LogError($"GameManager 이벤트 호출 오류: {d.Method.Name} -> {e}");
            }
        }
    }

    // 프로퍼티
    public GameState CurrentGameState => _currentGameState;
    public static int Stage
    {
        get => _stage;
        set => _stage = value;
    }
}
