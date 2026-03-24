using Photon.Pun;
using Photon.Realtime;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LobbyUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _readyBtn;
    [SerializeField] private Text _readyBtnText;
    [SerializeField] private Text _statusText;
    [SerializeField] private Text _countdownText;
    [SerializeField] private Text _playerCountText;
    [SerializeField] private Text _playerListText;

    [Header("Countdown Effect")]
    [SerializeField] private bool _hideCountdownWhenInactive = true;
    [SerializeField] private float _countdownPopScale = 1.35f;
    [SerializeField] private float _countdownPopSeconds = 0.18f;
    [SerializeField] private float _countdownShrinkSeconds = 0.12f;
    [SerializeField] private float _countdownStartScale = 0.85f;

    private int _lastCountdownSecond = -1;
    private Coroutine _countdownAnimCoroutine;

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
        UpdatePlayerCount();
        UpdatePlayerList();
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

    private void UpdatePlayerCount()
    {
        if (_playerCountText == null)
            return;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            _playerCountText.text = string.Empty;
            return;
        }

        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        _playerCountText.text = max > 0 ? $"{current}/{max}" : $"{current}";
    }

    private void UpdatePlayerList()
    {
        if (_playerListText == null)
            return;

        if (!PhotonNetwork.InRoom)
        {
            _playerListText.text = string.Empty;
            return;
        }

        Player[] players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < players.Length; i++)
        {
            string name = string.IsNullOrWhiteSpace(players[i].NickName) ? $"Player_{players[i].ActorNumber}" : players[i].NickName;
            sb.Append(name);
            if (i < players.Length - 1)
                sb.Append('\n');
        }

        _playerListText.text = sb.ToString();
    }

    private void UpdateCountdown()
    {
        if (_countdownText == null)
            return;

        if (NetworkAuthorityManager.Instance == null || !PhotonNetwork.InRoom)
        {
            SetCountdownVisible(false);
            return;
        }

        if (!NetworkAuthorityManager.Instance.TryGetLobbyCountdownRemaining(out float remaining, out bool isActive))
        {
            SetCountdownVisible(false);
            return;
        }

        if (!isActive)
        {
            SetCountdownVisible(false);
            return;
        }

        SetCountdownVisible(true);

        int secondsLeft = Mathf.Clamp(Mathf.CeilToInt(remaining), 0, 999);
        if (secondsLeft != _lastCountdownSecond)
        {
            _lastCountdownSecond = secondsLeft;
            _countdownText.text = secondsLeft.ToString();

            if (_countdownAnimCoroutine != null)
                StopCoroutine(_countdownAnimCoroutine);

            _countdownAnimCoroutine = StartCoroutine(PlayCountdownPop());
        }
    }

    private void SetCountdownVisible(bool isVisible)
    {
        if (_countdownText == null)
            return;

        if (_hideCountdownWhenInactive && _countdownText.gameObject.activeSelf != isVisible)
            _countdownText.gameObject.SetActive(isVisible);

        if (!isVisible)
        {
            _countdownText.text = string.Empty;
            _lastCountdownSecond = -1;

            if (_countdownAnimCoroutine != null)
            {
                StopCoroutine(_countdownAnimCoroutine);
                _countdownAnimCoroutine = null;
            }

            RectTransform rt = _countdownText.rectTransform;
            if (rt != null)
                rt.localScale = Vector3.one;
        }
    }

    private IEnumerator PlayCountdownPop()
    {
        if (_countdownText == null)
            yield break;

        RectTransform rt = _countdownText.rectTransform;
        if (rt == null)
            yield break;

        float start = Mathf.Max(0.01f, _countdownStartScale);
        float pop = Mathf.Max(start, _countdownPopScale);
        float popDur = Mathf.Max(0.01f, _countdownPopSeconds);
        float shrinkDur = Mathf.Max(0.01f, _countdownShrinkSeconds);

        // 커졌다가 다시 원래로 돌아오는 "팝" 효과
        float t = 0f;
        while (t < popDur)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / popDur);
            float s = Mathf.Lerp(start, pop, a);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        t = 0f;
        while (t < shrinkDur)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / shrinkDur);
            float s = Mathf.Lerp(pop, 1f, a);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        rt.localScale = Vector3.one;
        _countdownAnimCoroutine = null;
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
