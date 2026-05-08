using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// 달리기 경주 스테이지를 관리하는 매니저
/// - 가장 먼저 결승점에 도달한 플레이어가 승리
/// </summary>
public class RaceStageManager : MonoBehaviourPunCallbacks
{
    public static RaceStageManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float _gameDuration = 300f; // 5분
    [SerializeField] private bool _isGameStarted = false;
    [SerializeField] private bool _isGameEnded = false;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private GameObject _resultPanel;
    [SerializeField] private TextMeshProUGUI _winnerText;

    [Header("Player Tracking")]
    private List<Player> _rankings = new List<Player>();
    private float _elapsedTime = 0f;

    public System.Action OnRaceStart;
    public System.Action<Player> OnRaceFinished;

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
        if (_resultPanel != null) _resultPanel.SetActive(false);
    }

    private void Update()
    {
        if (!_isGameStarted || _isGameEnded) return;

        _elapsedTime += Time.deltaTime;
        UpdateUI();

        if (_elapsedTime >= _gameDuration)
        {
            EndRace();
        }
    }

    public void StartRace()
    {
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.InRoom)
                photonView.RPC(nameof(RPC_StartRace), RpcTarget.All);
            else
                StartRaceInternal();
        }
    }

    [PunRPC]
    private void RPC_StartRace() { StartRaceInternal(); }

    private void StartRaceInternal()
    {
        _isGameStarted = true;
        _isGameEnded = false;
        _elapsedTime = 0f;
        _rankings.Clear();
        
        Debug.Log("[RaceStageManager] Race Started!");
        OnRaceStart?.Invoke();
    }

    /// <summary>
    /// 플레이어가 결승선을 통과했을 때 호출
    /// </summary>
    public void PlayerReachedFinishLine(Player player)
    {
        if (!_isGameStarted || _isGameEnded) return;

        if (PhotonNetwork.InRoom)
        {
            // 중복 체크 방지
            bool alreadyFinished = false;
            foreach (var p in _rankings)
            {
                if (p.ActorNumber == player.ActorNumber) { alreadyFinished = true; break; }
            }

            if (!alreadyFinished)
            {
                photonView.RPC(nameof(RPC_PlayerFinished), RpcTarget.All, player.ActorNumber);
            }
        }
        else
        {
            _rankings.Add(player);
            EndRace();
        }
    }

    [PunRPC]
    private void RPC_PlayerFinished(int actorNumber)
    {
        Player finishedPlayer = null;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber)
            {
                finishedPlayer = p;
                break;
            }
        }

        if (finishedPlayer != null && !_rankings.Contains(finishedPlayer))
        {
            _rankings.Add(finishedPlayer);
            Debug.Log($"[Race] {finishedPlayer.NickName} reached finish line! Rank: {_rankings.Count}");

            // 첫 번째 도착자가 발생하면 게임 종료 (또는 일정 시간 후 종료)
            if (_rankings.Count == 1)
            {
                // 1등이 나오면 5초 뒤 종료 등의 로직 추가 가능
                Invoke(nameof(EndRace), 2f); 
            }
        }
    }

    public void EndRace()
    {
        if (_isGameEnded) return;

        _isGameEnded = true;
        _isGameStarted = false;

        Debug.Log("[RaceStageManager] Race Ended!");
        ShowResult();
    }

    private void ShowResult()
    {
        if (_resultPanel != null) _resultPanel.SetActive(true);

        Player winner = _rankings.Count > 0 ? _rankings[0] : null;
        if (_winnerText != null)
        {
            _winnerText.text = winner != null ? $"Winner: {winner.NickName}" : "No Winner";
        }
    }

    private void UpdateUI()
    {
        if (_timerText != null)
        {
            float remain = Mathf.Max(0, _gameDuration - _elapsedTime);
            int min = Mathf.FloorToInt(remain / 60f);
            int sec = Mathf.FloorToInt(remain % 60f);
            _timerText.text = $"{min:D2}:{sec:D2}";
        }
    }

    public void ReturnToLobby()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LoadLevel("Scene_Lobby");
        else UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_Lobby");
    }
}
