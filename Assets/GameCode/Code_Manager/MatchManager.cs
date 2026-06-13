using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public static MatchManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float _gameDuration = 180f; // 3분 기본
    [SerializeField] private float _timeLimit = 0f; // 0이면 제한 없음
    [SerializeField] private bool _useTimeLimit = false;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private TextMeshProUGUI _tileCountText;
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TextMeshProUGUI _winnerText;
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private Button _returnToLobbyButton;

    [Header("States")]
    [SerializeField] private bool _isGameStarted = false;
    [SerializeField] private bool _isGameEnded = false;
    [SerializeField] private float _remainingTime = 0f;

    // 현재 게임 상태
    public enum GameState
    {
        Waiting,
        Playing,
        Result,
        LobbyReturn
    }

    [SerializeField] private GameState _currentState = GameState.Waiting;
    public GameState CurrentState => _currentState;

    // winner 정보
    private Player _winner;

    // 이벤트
    public System.Action OnGameStart;
    public System.Action OnGameEnd;
    public System.Action<Player> OnWinnerDeclared;
    public System.Action<float> OnTimerUpdate; // (남은 시간)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _remainingTime = _gameDuration;

        // 버튼 이벤트
        if (_returnToLobbyButton != null)
        {
            _returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        }

        // 초기 UI 숨김
        if (_resultPanel != null)
            _resultPanel.SetActive(false);

        // HexArenaManager 이벤트 구독
        if (HexArenaManager.Instance != null)
        {
            HexArenaManager.Instance.OnSinglePlayerLeft += HandleSinglePlayerLeft;
            HexArenaManager.Instance.OnLastTileSunk += HandleLastTileSunk;
        }
    }

    private void Update()
    {
        if (!_isGameStarted || _isGameEnded)
            return;

        // 타이머 업데이트
        UpdateTimer();

        // UI 업데이트
        UpdateUI();
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (HexArenaManager.Instance != null)
        {
            HexArenaManager.Instance.OnSinglePlayerLeft -= HandleSinglePlayerLeft;
            HexArenaManager.Instance.OnLastTileSunk -= HandleLastTileSunk;
        }
    }

    #region Game Flow

    // 게임 시작 (마스터에서 호출)
    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
        {
            StartGameInternal();

            // 네트워크 동기화
            if (PhotonNetwork.InRoom)
            {
                photonView.RPC(nameof(RPC_StartGame), RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void RPC_StartGame()
    {
        StartGameInternal();
    }

    private void StartGameInternal()
    {
        _isGameStarted = true;
        _isGameEnded = false;
        _currentState = GameState.Playing;
        _remainingTime = _gameDuration;

        // 아레나 매니저에 게임 시작 알림
        if (HexArenaManager.Instance != null)
        {
            HexArenaManager.Instance.StartGame();
        }

        Debug.Log("[HexGameManager] Game started!");
        OnGameStart?.Invoke();
    }

    // 게임 종료
    public void EndGame()
    {
        if (_isGameEnded)
            return;

        _isGameEnded = true;
        _isGameStarted = false;
        _currentState = GameState.Result;

        // 아레나 매니저에 게임 종료 알림
        if (HexArenaManager.Instance != null)
        {
            HexArenaManager.Instance.EndGame();
        }

        Debug.Log("[HexGameManager] Game ended!");
        OnGameEnd?.Invoke();

        // Result UI 표시
        ShowResult();
    }

    // Result 화면 표시
    private void ShowResult()
    {
        if (_resultPanel != null)
        {
            _resultPanel.SetActive(true);
        }

        // 승자 결정
        DetermineWinner();
    }

    // 승자 결정
    private void DetermineWinner()
    {
        Player winner = null;

        if (PhotonNetwork.InRoom)
        {
            // Photon的玩家 목록에서 마지막 생존자 찾기
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p != null)
                {
                    winner = p;
                    break;
                }
            }
        }

        _winner = winner;

        // UI 업데이트
        if (_winnerText != null)
        {
            string winnerName = winner?.NickName ?? "Unknown";
            _winnerText.text = $"WINNER: {winnerName}";
        }

        if (_rankText != null)
        {
            _rankText.text = $"1위: {winner?.NickName ?? "Unknown"}";
        }

        Debug.Log($"[HexGameManager] Winner declared: {_winner?.NickName}");
        OnWinnerDeclared?.Invoke(_winner);

        // 네트워크 동기화
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            int winnerId = winner != null ? winner.ActorNumber : -1;
            photonView.RPC(nameof(RPC_WinnerDeclared), RpcTarget.All, winnerId);
        }
    }

    [PunRPC]
    private void RPC_WinnerDeclared(int winnerActorNumber)
    {
        Player winner = null;
        if (winnerActorNumber >= 0)
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == winnerActorNumber)
                {
                    winner = p;
                    break;
                }
            }
        }

        _winner = winner;

        if (_winnerText != null)
        {
            _winnerText.text = $"WINNER: {_winner?.NickName ?? "Unknown"}";
        }
    }

    // 로비로 복귀
    public void ReturnToLobby()
    {
        _currentState = GameState.LobbyReturn;

        if (PhotonNetwork.InRoom)
        {
            // 마스터가 Lobby로 복귀 처리
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("Scene_Lobby");
            }
            else
            {
                // 일반 플레이어는 마스터가 처리할 때까지 대기
                PhotonNetwork.LoadLevel("Scene_Lobby");
            }
        }
        else
        {
            // 비네트워크 환경
            UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_Lobby");
        }
    }

    #endregion

    #region Event Handlers

    // 마지막 플레이어만 남았을 때
    private void HandleSinglePlayerLeft()
    {
        if (!_isGameStarted || _isGameEnded)
            return;

        Debug.Log("[HexGameManager] Single player left - ending game");
        EndGame();
    }

    // 마지막 타일이 가라앉았을 때
    private void HandleLastTileSunk()
    {
        if (!_isGameStarted || _isGameEnded)
            return;

        // 마지막 타일 위의 플레이어가 승리
        Debug.Log("[HexGameManager] Last tile sunk - ending game");
        EndGame();
    }

    #endregion

    #region Timer
    // 타이머 업데이트
    private void UpdateTimer()
    {
        if (_useTimeLimit && _timeLimit > 0)
        {
            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0)
            {
                _remainingTime = 0;
                Debug.Log("[HexGameManager] Time limit reached");
                EndGame();
            }
        }
        else
        {
            // カウントダウン方式
            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0)
            {
                _remainingTime = 0;
            }
        }

        OnTimerUpdate?.Invoke(_remainingTime);
    }

    // 타이머 텍스트 포맷
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:D2}:{seconds:D2}";
    }

    #endregion

    #region UI Updates

    // UI 업데이트
    private void UpdateUI()
    {
        // 타이머
        if (_timerText != null)
        {
            _timerText.text = FormatTime(_remainingTime);

            // 시간에 따른 색상变化
            if (_remainingTime < 30f)
                _timerText.color = Color.red;
            else if (_remainingTime < 60f)
                _timerText.color = Color.yellow;
            else
                _timerText.color = Color.white;
        }

        if (_playerCountText != null && HexArenaManager.Instance != null)
        {
            _playerCountText.text = $"Players: {HexArenaManager.Instance.AlivePlayerCount}";
        }

        if (_tileCountText != null && HexArenaManager.Instance != null)
        {
            _tileCountText.text = $"Tiles: {HexArenaManager.Instance.ActiveTileCount}";
        }
    }

    #endregion

    #region Photon Callbacks

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log($"[HexGameManager] Player left: {otherPlayer.NickName}");

        // 남은 플레이어 체크
        if (PhotonNetwork.PlayerList.Length <= 1 && _isGameStarted && !_isGameEnded)
        {
            EndGame();
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        ResetGame();
    }

    #endregion

    #region Helper Methods

    // 게임 리셋
    public void ResetGame()
    {
        _isGameStarted = false;
        _isGameEnded = false;
        _currentState = GameState.Waiting;
        _remainingTime = _gameDuration;
        _winner = null;

        if (_resultPanel != null)
            _resultPanel.SetActive(false);

        if (HexArenaManager.Instance != null)
        {
            HexArenaManager.Instance.EndGame();
            HexArenaManager.Instance.ResetAllTiles();
        }
    }

    // 현재 승자 가져오기
    public Player GetWinner()
    {
        return _winner;
    }

    // 게임 통계 반환
    public string GetGameStats()
    {
        return $"[HexGame] State: {_currentState}, Time: {FormatTime(_remainingTime)}, " +
               $"Players: {HexArenaManager.Instance?.AlivePlayerCount ?? 0}, " +
               $"Tiles: {HexArenaManager.Instance?.ActiveTileCount ?? 0}";
    }

    #endregion
}
