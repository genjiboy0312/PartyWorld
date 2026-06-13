using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
using EventCode = System.Byte;
using ExitGames.Client.Photon;

public class RoundManager : MonoBehaviour, IOnEventCallback
{
    public static RoundManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    // RaiseEvent 채널
    // ──────────────────────────────────────────────
    public const EventCode EVENT_START_ROUND = 20;
    public const EventCode EVENT_PLAYER_FINISHED = 21;
    public const EventCode EVENT_PLAYER_ELIMINATED = 22;
    public const EventCode EVENT_ROUND_ENDED = 23;
    public const EventCode EVENT_RETURN_TO_LOBBY = 24;

    // ──────────────────────────────────────────────
    // Inspector Settings
    // ──────────────────────────────────────────────
    [Header("Round Settings")]
    [SerializeField] private int _totalRounds = 4;
    [SerializeField] private float _introDelaySeconds = 3f;
    [SerializeField] private float _resultDisplaySeconds = 5f;
    [SerializeField] private float _countdownWarningSeconds = 10f;

    [Header("Scene Names")]
    [SerializeField] private string _resultSceneName = "Scene_Result";
    [SerializeField] private string _lobbySceneName = "Scene_Lobby";

    [Header("Scoring")]
    [SerializeField] private int[] _rankScores = { 10, 8, 6, 5, 4, 3, 2, 1, 1, 1, 1, 1 };

    // ──────────────────────────────────────────────
    // Runtime State
    // ──────────────────────────────────────────────
    private int _currentRoundIndex = -1;
    private float _roundStartTime;
    private float _roundTimeLimit;
    private GameMode _currentGameMode = GameMode.Race;
    private bool _isRoundActive;

    private List<int> _finishOrder = new List<int>(12);
    private List<int> _eliminatedOrder = new List<int>(12);
    private HashSet<int> _finishedSet = new HashSet<int>();
    private HashSet<int> _eliminatedSet = new HashSet<int>();

    // 누적 스코어: actorNumber → 총점
    private Dictionary<int, int> _cumulativeScores = new Dictionary<int, int>();
    // 총 라운드 결과 저장
    private List<RoundResults> _completedRounds = new List<RoundResults>();

    // 현재 라운드에서 제거된 플레이어는 더 이상 진행 불가
    // 이 라운드에 참여 중인 플레이어 목록 (ActorNumber)
    private List<int> _activePlayerList = new List<int>();

    private Coroutine _roundTimerCoroutine;
    private Coroutine _introCoroutine;

    // ──────────────────────────────────────────────
    // RoundResults — 단일 라운드 결과 컨테이너
    // ──────────────────────────────────────────────
    public class RoundResults
    {
        public int roundIndex;
        public GameMode gameMode;
        public PlayerRoundResult[] results;
        public string mapSceneName;
    }

    // ──────────────────────────────────────────────
    // MonoBehaviour
    // ──────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ──────────────────────────────────────────────
    // Public API — NetworkAuthorityManager가 호출
    // ──────────────────────────────────────────────

    /// <summary>
    /// 맵 씬이 로드된 후 호출됨. 라운드 세션을 초기화하고 시작 준비.
    /// </summary>
    public void InitializeRoundSession(string mapSceneName)
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[RoundManager] Not in a room. Can't start round.");
            return;
        }

        _currentRoundIndex++;

        // 첫 라운드면 누적 데이터 초기화
        if (_currentRoundIndex == 0)
        {
            _cumulativeScores.Clear();
            _completedRounds.Clear();
            _activePlayerList.Clear();

            // 모든 플레이어를 활성 목록에 추가
            foreach (Player p in PhotonNetwork.PlayerList)
                _activePlayerList.Add(p.ActorNumber);
        }

        _finishOrder.Clear();
        _eliminatedOrder.Clear();
        _finishedSet.Clear();
        _eliminatedSet.Clear();

        _isRoundActive = false;

        // 게임 모드 결정 (현재는 Scene_Map prefix 기반. 나중에 확장)
        _currentGameMode = GetGameModeFromScene(mapSceneName);
        _roundTimeLimit = ResolveTimeLimit(_currentGameMode);

        Debug.Log($"[RoundManager] Round {_currentRoundIndex + 1}/{_totalRounds} initializing. " +
            $"Mode={_currentGameMode}, Map={mapSceneName}, ActivePlayers={_activePlayerList.Count}");

        // 모든 클라이언트에 라운드 시작 준비 알림
        StartIntro();
    }

    /// <summary>
    /// 로컬 플레이어가 결승선 통과 시 호출.
    /// </summary>
    public void ReportPlayerFinished(int actorNumber)
    {
        if (!_isRoundActive)
            return;

        // 마스터에게 전송
        PhotonNetwork.RaiseEvent(
            EVENT_PLAYER_FINISHED,
            actorNumber,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            SendOptions.SendReliable
        );
    }

    /// <summary>
    /// 로컬 플레이어가 낙사/제거 시 호출.
    /// </summary>
    public void ReportPlayerEliminated(int actorNumber)
    {
        if (!_isRoundActive)
            return;

        // 마스터에게 전송
        PhotonNetwork.RaiseEvent(
            EVENT_PLAYER_ELIMINATED,
            actorNumber,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            SendOptions.SendReliable
        );
    }

    /// <summary>
    /// 모든 라운드가 종료되면 호출되어 결과 씬으로 이동.
    /// </summary>
    public void ReturnToLobby()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        PhotonNetwork.RaiseEvent(
            EVENT_RETURN_TO_LOBBY,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );
    }

    /// <summary>
    /// 세션 종료 시 호출. 모든 라운드 상태 초기화 (로비 복귀 시).
    /// </summary>
    public void CleanupSession()
    {
        _isRoundActive = false;
        _currentRoundIndex = -1;
        _finishOrder.Clear();
        _eliminatedOrder.Clear();
        _finishedSet.Clear();
        _eliminatedSet.Clear();
        _cumulativeScores.Clear();
        _completedRounds.Clear();
        _activePlayerList.Clear();

        if (_roundTimerCoroutine != null)
        {
            StopCoroutine(_roundTimerCoroutine);
            _roundTimerCoroutine = null;
        }
        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _introCoroutine = null;
        }

        Debug.Log("[RoundManager] Session cleaned up.");
    }

    // ──────────────────────────────────────────────
    // Intro / Start
    // ──────────────────────────────────────────────
    private void StartIntro()
    {
        if (_introCoroutine != null)
            StopCoroutine(_introCoroutine);
        _introCoroutine = StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // "Round X" 표시 (모든 클라이언트)
        string msg = $"Round {_currentRoundIndex + 1}";
        PhotonNetwork.RaiseEvent(
            EVENT_START_ROUND,
            msg,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );

        // 인트로 카운트다운 (3초)
        yield return new WaitForSeconds(_introDelaySeconds);

        // 라운드 시작
        _roundStartTime = Time.time;
        _isRoundActive = true;

        Debug.Log($"[RoundManager] Round {_currentRoundIndex + 1} started! Mode={_currentGameMode}, TimeLimit={_roundTimeLimit}s");

        // 라운드 타이머 시작
        if (_roundTimerCoroutine != null)
            StopCoroutine(_roundTimerCoroutine);
        _roundTimerCoroutine = StartCoroutine(RoundTimer());
    }

    private IEnumerator RoundTimer()
    {
        float elapsed = 0f;
        bool warned = false;

        while (_isRoundActive)
        {
            elapsed = Time.time - _roundStartTime;

            // Check round end conditions
            if (elapsed >= _roundTimeLimit)
            {
                Debug.Log($"[RoundManager] Round time limit reached ({_roundTimeLimit}s)");
                EndRound();
                yield break;
            }

            // 마스터만 라운드 종료 조건 확인
            if (PhotonNetwork.IsMasterClient)
            {
                if (CheckRoundEndCondition(elapsed))
                {
                    EndRound();
                    yield break;
                }
            }

            // 10초 경고
            if (!warned && elapsed >= _roundTimeLimit - _countdownWarningSeconds)
            {
                warned = true;
                Debug.Log($"[RoundManager] 10 seconds remaining!");
            }

            yield return null;
        }
    }

    private bool CheckRoundEndCondition(float elapsed)
    {
        int totalActive = _activePlayerList.Count;
        int finished = _finishOrder.Count;
        int eliminated = _eliminatedOrder.Count;
        int remaining = totalActive - eliminated;

        switch (_currentGameMode)
        {
            case GameMode.Race:
                // Everyone finished or time up
                return finished >= totalActive;

            case GameMode.Survival:
                // One or fewer survivors
                return remaining <= 1;

            case GameMode.Score:
                // Time-based — timer handles this
                return false;

            case GameMode.Team:
                return false;

            default:
                return false;
        }
    }

    // ──────────────────────────────────────────────
    // End Round
    // ──────────────────────────────────────────────
    private void EndRound()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        _isRoundActive = false;

        if (_roundTimerCoroutine != null)
        {
            StopCoroutine(_roundTimerCoroutine);
            _roundTimerCoroutine = null;
        }

        // Generate results
        PlayerRoundResult[] results = GenerateResults();
        var roundResults = new RoundResults
        {
            roundIndex = _currentRoundIndex,
            gameMode = _currentGameMode,
            results = results,
            mapSceneName = SceneManager.GetActiveScene().name
        };
        _completedRounds.Add(roundResults);

        // Update cumulative scores
        foreach (var r in results)
        {
            if (!_cumulativeScores.ContainsKey(r.actorNumber))
                _cumulativeScores[r.actorNumber] = 0;
            _cumulativeScores[r.actorNumber] += r.score;
        }

        // 로그 출력
        Debug.Log($"[RoundManager] Round {_currentRoundIndex + 1} ended. Results:");
        foreach (var r in results)
        {
            Debug.Log($"  Actor {r.actorNumber}: Rank={r.rank}, Score={r.score}, " +
                $"Finished={r.finished}, Eliminated={r.eliminated}");
        }

        // Serialize results for event
        object[] resultData = SerializeResults(results);

        // Broadcast to all clients
        PhotonNetwork.RaiseEvent(
            EVENT_ROUND_ENDED,
            resultData,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );

        // Decide next action
        StartCoroutine(PostRoundSequence());
    }

    private IEnumerator PostRoundSequence()
    {
        // Show results briefly
        yield return new WaitForSeconds(_resultDisplaySeconds);

        // Check if we should continue or end the session
        bool isLastRound = _currentRoundIndex >= _totalRounds - 1;

        if (isLastRound)
        {
            // Move to result scene
            Debug.Log("[RoundManager] All rounds complete! Moving to result scene.");
            GoToResultScene();
        }
        else
        {
            // Next round — back to lobby for re-lobbying or directly to next map
            // For now, return to lobby for simplicity
            Debug.Log($"[RoundManager] Round {_currentRoundIndex + 1} complete. Returning to lobby for next round.");
            PhotonNetwork.LoadLevel(_lobbySceneName);
        }
    }

    private void GoToResultScene()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        // Set GameManager state
        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameState.Result);

        PhotonNetwork.LoadLevel(_resultSceneName);
    }

    // ──────────────────────────────────────────────
    // Results Generation
    // ──────────────────────────────────────────────
    private PlayerRoundResult[] GenerateResults()
    {
        int totalPlayers = _activePlayerList.Count;
        var results = new List<PlayerRoundResult>();

        // 현재 라운드 게임 모드에 따라 결과 생성
        switch (_currentGameMode)
        {
            case GameMode.Race:
                GenerateRaceResults(results, totalPlayers);
                break;
            case GameMode.Survival:
                GenerateSurvivalResults(results, totalPlayers);
                break;
            default:
                GenerateRaceResults(results, totalPlayers);
                break;
        }

        // 결과 정렬 (1등부터)
        results.Sort((a, b) => a.rank.CompareTo(b.rank));

        return results.ToArray();
    }

    private void GenerateRaceResults(List<PlayerRoundResult> results, int totalPlayers)
    {
        int rank = 1;

        // 결승선 통과 순서대로 순위
        foreach (int actorNr in _finishOrder)
        {
            results.Add(MakeResult(actorNr, rank, true, false));
            rank++;
        }

        // 제거된 플레이어
        foreach (int actorNr in _eliminatedOrder)
        {
            results.Add(MakeResult(actorNr, rank, false, true));
            rank++;
        }

        // 나머지 (결승선 미통과, 제거 안 됨)
        foreach (int actorNr in _activePlayerList)
        {
            if (!_finishedSet.Contains(actorNr) && !_eliminatedSet.Contains(actorNr))
            {
                results.Add(MakeResult(actorNr, rank, false, false));
                rank++;
            }
        }

        // 부족한 플레이어 수만큼 채우기
        while (rank <= totalPlayers)
        {
            results.Add(MakeResult(rank, rank, false, false));
            rank++;
        }
    }

    private void GenerateSurvivalResults(List<PlayerRoundResult> results, int totalPlayers)
    {
        int rank = 1;

        // 생존자 (제거 안 됨)
        foreach (int actorNr in _activePlayerList)
        {
            if (!_eliminatedSet.Contains(actorNr))
            {
                results.Add(MakeResult(actorNr, rank, true, false));
                rank++;
            }
        }

        // 제거된 플레이어 (늦게 제거된 순 = 높은 순위)
        for (int i = _eliminatedOrder.Count - 1; i >= 0; i--)
        {
            int actorNr = _eliminatedOrder[i];
            results.Add(MakeResult(actorNr, rank, false, true));
            rank++;
        }
    }

    private PlayerRoundResult MakeResult(int actorNumber, int rank, bool finished, bool eliminated)
    {
        string name = "Player";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber)
            {
                name = p.NickName;
                break;
            }
        }

        return new PlayerRoundResult
        {
            actorNumber = actorNumber,
            nickName = name,
            rank = rank,
            score = GetRankScore(rank),
            finished = finished,
            eliminated = eliminated,
            finishTime = rank * 10f
        };
    }

    private int GetRankScore(int rank)
    {
        if (rank >= 1 && rank <= _rankScores.Length)
            return _rankScores[rank - 1];
        return 1;
    }

    // ──────────────────────────────────────────────
    // RaiseEvent 수신
    // ──────────────────────────────────────────────
    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case EVENT_PLAYER_FINISHED:
                HandlePlayerFinishedEvent(photonEvent);
                break;

            case EVENT_PLAYER_ELIMINATED:
                HandlePlayerEliminatedEvent(photonEvent);
                break;

            case EVENT_ROUND_ENDED:
                HandleRoundEndedEvent(photonEvent);
                break;

            case EVENT_RETURN_TO_LOBBY:
                HandleReturnToLobby();
                break;
        }
    }

    private void HandlePlayerFinishedEvent(EventData photonEvent)
    {
        if (photonEvent.CustomData is int actorNumber)
        {
            if (_finishedSet.Add(actorNumber))
            {
                _finishOrder.Add(actorNumber);
                Debug.Log($"[RoundManager] Player {actorNumber} finished. Order={_finishOrder.Count}");

                // Race mode: check if round should end
                if (PhotonNetwork.IsMasterClient && _currentGameMode == GameMode.Race)
                {
                    if (_finishOrder.Count >= _activePlayerList.Count - _eliminatedOrder.Count)
                        EndRound();
                }
            }
        }
    }

    private void HandlePlayerEliminatedEvent(EventData photonEvent)
    {
        if (photonEvent.CustomData is int actorNumber)
        {
            if (_eliminatedSet.Add(actorNumber))
            {
                _eliminatedOrder.Add(actorNumber);
                Debug.Log($"[RoundManager] Player {actorNumber} eliminated. Count={_eliminatedOrder.Count}");

                // Survival mode: check if round should end
                if (PhotonNetwork.IsMasterClient && _currentGameMode == GameMode.Survival)
                {
                    int remaining = _activePlayerList.Count - _eliminatedOrder.Count;
                    if (remaining <= 1)
                        EndRound();
                }
            }
        }
    }

    private void HandleRoundEndedEvent(EventData photonEvent)
    {
        _isRoundActive = false;

        if (_roundTimerCoroutine != null)
        {
            StopCoroutine(_roundTimerCoroutine);
            _roundTimerCoroutine = null;
        }

        // Deserialize results
        if (photonEvent.CustomData is object[] resultData)
        {
            PlayerRoundResult[] results = DeserializeResults(resultData);
            Debug.Log($"[RoundManager] Received round end results. Count={results.Length}");
        }
    }

    private void HandleReturnToLobby()
    {
        _isRoundActive = false;
        Debug.Log("[RoundManager] Returning to lobby.");

        if (GameManager.Instance != null)
            GameManager.Instance.SyncStateByScene(_lobbySceneName);

        PhotonNetwork.LoadLevel(_lobbySceneName);
    }

    // ──────────────────────────────────────────────
    // Serialization (simple approach)
    // ──────────────────────────────────────────────
    private object[] SerializeResults(PlayerRoundResult[] results)
    {
        // Send as flat array: [actorNumber, rank, score, flags, ...]
        var data = new object[results.Length * 4];
        for (int i = 0; i < results.Length; i++)
        {
            int idx = i * 4;
            data[idx] = results[i].actorNumber;
            data[idx + 1] = results[i].rank;
            data[idx + 2] = results[i].score;
            data[idx + 3] = results[i].finished ? 1 : 0; // bool as int
        }
        return data;
    }

    private PlayerRoundResult[] DeserializeResults(object[] data)
    {
        if (data == null || data.Length < 4)
            return System.Array.Empty<PlayerRoundResult>();

        int count = data.Length / 4;
        var results = new PlayerRoundResult[count];

        for (int i = 0; i < count; i++)
        {
            int idx = i * 4;
            results[i] = new PlayerRoundResult
            {
                actorNumber = (int)data[idx],
                rank = (int)data[idx + 1],
                score = (int)data[idx + 2],
                finished = (int)data[idx + 3] == 1
            };
        }
        return results;
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────
    private GameMode GetGameModeFromScene(string sceneName)
    {
        // Scene naming convention:
        // Scene_Map_Race_*, Scene_Map_Survive_*, Scene_Map_Score_*, Scene_Map_Team_*
        if (sceneName.Contains("Survive", System.StringComparison.OrdinalIgnoreCase))
            return GameMode.Survival;
        if (sceneName.Contains("Score", System.StringComparison.OrdinalIgnoreCase))
            return GameMode.Score;
        if (sceneName.Contains("Team", System.StringComparison.OrdinalIgnoreCase))
            return GameMode.Team;

        return GameMode.Race; // default
    }

    private float ResolveTimeLimit(GameMode mode)
    {
        return mode switch
        {
            GameMode.Race => 180f,
            GameMode.Survival => 120f,
            GameMode.Score => 120f,
            GameMode.Team => 120f,
            _ => 180f
        };
    }

    /// <summary>
    /// 결과 데이터 조회 (누적 스코어 + 전체 결과)
    /// </summary>
    public Dictionary<int, int> GetCumulativeScores() => new Dictionary<int, int>(_cumulativeScores);
    public bool IsRoundActive => _isRoundActive;
    public int CurrentRoundIndex => _currentRoundIndex;
    public int TotalRounds => _totalRounds;
    public List<RoundResults> GetCompletedRounds() => _completedRounds;
}
