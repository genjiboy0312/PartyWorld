using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// Hex-A-Gone 게임의 전체 흐름(UI, 타이머, 승리 판정)을 관리하는 스테이지 매니저
/// </summary>
public class HexaStageManager : MonoBehaviourPunCallbacks
{
    public static HexaStageManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float _gameDuration = 180f;
    [SerializeField] private bool _useTimeLimit = true;

    [Header("UI References")]
    [SerializeField] private Text _timerText;
    [SerializeField] private Text _playerCountText;
    [SerializeField] private GameObject _resultPanel;
    // [SerializeField] private TextMeshProUGUI _winnerText;

    [Header("States")]
    [SerializeField] private bool _isGameStarted = false;
    [SerializeField] private bool _isGameEnded = false;
    [SerializeField] private float _remainingTime = 0f;

    public enum HexGameState { Waiting, Playing, Result }
    [SerializeField] private HexGameState _currentState = HexGameState.Waiting;

    private Player _winner;

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
        if (_resultPanel != null) _resultPanel.SetActive(false);

        if (HexArenaManager.Instance != null)
        {
            HexArenaManager.Instance.OnSinglePlayerLeft += HandleSinglePlayerLeft;
            HexArenaManager.Instance.OnLastTileSunk += HandleLastTileSunk;
        }
    }

    private void Update()
    {
        if (!_isGameStarted || _isGameEnded) return;

        UpdateTimer();
        UpdateUI();
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.InRoom)
                photonView.RPC(nameof(RPC_StartGame), RpcTarget.All);
            else
                StartGameInternal();
        }
    }

    [PunRPC]
    private void RPC_StartGame() { StartGameInternal(); }

    private void StartGameInternal()
    {
        _isGameStarted = true;
        _isGameEnded = false;
        _currentState = HexGameState.Playing;
        _remainingTime = _gameDuration;

        if (HexArenaManager.Instance != null)
            HexArenaManager.Instance.StartGame();
    }

    public void EndGame()
    {
        if (_isGameEnded) return;

        _isGameEnded = true;
        _isGameStarted = false;
        _currentState = HexGameState.Result;

        if (HexArenaManager.Instance != null)
            HexArenaManager.Instance.EndGame();

        ShowResult();
    }

    private void ShowResult()
    {
        if (_resultPanel != null) _resultPanel.SetActive(true);
        DetermineWinner();
    }

    private void DetermineWinner()
    {
        // 현재 살아남은 플레이어 중 한 명을 승자로 (간단한 로직)
        if (PhotonNetwork.InRoom)
        {
            _winner = PhotonNetwork.LocalPlayer; // 기본값
        }

        // if (_winnerText != null)
        //     _winnerText.text = _winner != null ? $"Winner: {_winner.NickName}" : "No Winner";
    }

    private void UpdateTimer()
    {
        if (_useTimeLimit)
        {
            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0)
            {
                _remainingTime = 0;
                EndGame();
            }
        }
    }

    private void UpdateUI()
    {
        if (_timerText != null)
        {
            int min = Mathf.FloorToInt(_remainingTime / 60f);
            int sec = Mathf.FloorToInt(_remainingTime % 60f);
            _timerText.text = $"{min:D2}:{sec:D2}";
        }

        if (_playerCountText != null && HexArenaManager.Instance != null)
            _playerCountText.text = $"Players: {HexArenaManager.Instance.AlivePlayerCount}";
    }

    private void HandleSinglePlayerLeft() { EndGame(); }
    private void HandleLastTileSunk() { EndGame(); }

    public void ReturnToLobby()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LoadLevel("Scene_Lobby");
        else SceneManager.LoadScene("Scene_Lobby");
    }
}
