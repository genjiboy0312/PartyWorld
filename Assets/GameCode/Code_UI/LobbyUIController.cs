using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _readyBtn;
    [SerializeField] private Text _readyBtnText;
    [SerializeField] private Text _statusText;
    [SerializeField] private Text _countdownText;

    private void Start()
    {
        // 버튼 클릭 시 Ready 토글
        if (_readyBtn != null)
            _readyBtn.onClick.AddListener(OnReadyClicked);
    }

    private void OnDestroy()
    {
        // 씬 전환 시 이벤트 정리
        if (_readyBtn != null)
            _readyBtn.onClick.RemoveListener(OnReadyClicked);
    }

    private void Update()
    {
        // 네트워크 상태/카운트다운을 매 프레임 UI에 반영
        UpdateStatus();
        UpdateCountdown();
        UpdateReadyLabel();
    }

    private void OnReadyClicked()
    {
        // 룸 안에서만 Ready를 토글
        if (!PhotonNetwork.InRoom)
            return;

        NetworkAuthorityManager.Instance?.ToggleReady();
    }

    private void UpdateStatus()
    {
        if (_statusText == null)
            return;

        if (!PhotonNetwork.IsConnected)
        {
            _statusText.text = "Disconnected";
            return;
        }

        string room = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom?.Name ?? "Room" : "NoRoom";
        int count = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom?.PlayerCount ?? 0 : 0;
        _statusText.text = $"{PhotonNetwork.NetworkClientState} / {room} ({count})";
    }

    private void UpdateCountdown()
    {
        if (_countdownText == null)
            return;

        if (NetworkAuthorityManager.Instance == null || !PhotonNetwork.InRoom)
        {
            _countdownText.text = string.Empty;
            return;
        }

        if (!NetworkAuthorityManager.Instance.TryGetLobbyCountdownRemaining(out float remaining, out bool isActive))
        {
            _countdownText.text = string.Empty;
            return;
        }

        if (!isActive)
        {
            _countdownText.text = string.Empty;
            return;
        }

        _countdownText.text = $"Starting in {Mathf.CeilToInt(remaining)}";
    }

    private void UpdateReadyLabel()
    {
        if (_readyBtnText == null)
            return;

        if (!PhotonNetwork.InRoom)
        {
            _readyBtnText.text = "Ready";
            return;
        }

        bool isReady = false;
        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("ready", out object raw) &&
            raw is bool b)
        {
            isReady = b;
        }

        _readyBtnText.text = isReady ? "Unready" : "Ready";
    }
}

