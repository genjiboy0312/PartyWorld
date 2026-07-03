using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class WaitingRoomUIController : MonoBehaviour, IInRoomCallbacks
{
    private const string PlayerPropReady = "ready";
    private const string RoomPropCountdownActive = "lobbyCountdownActive";
    private const string RoomPropCountdownStartTime = "lobbyCountdownStartTime";
    private const string RoomPropCountdownDuration = "lobbyCountdownDuration";

    [Header("UI References")]
    [SerializeField] private Button _readyButton;
    [SerializeField] private Text _textReady;
    [SerializeField] private Text _textCountdown;
    [SerializeField] private Text _textPlayerCount;
    [SerializeField] private RectTransform _playerListScrollView;
    [SerializeField] private GameObject _playerEntryTemplate;

    [Header("Countdown Effect")]
    [SerializeField] private bool _hideCountdownWhenInactive = true;
    [SerializeField] private float _countdownPopScale = 1.35f;
    [SerializeField] private float _countdownPopSeconds = 0.18f;
    [SerializeField] private float _countdownShrinkSeconds = 0.12f;
    [SerializeField] private float _countdownStartScale = 0.85f;
    [SerializeField] private float _fallbackCountdownSeconds = 5f;
    [SerializeField] private string _fallbackLoadingSceneName = "Scene_Loading";
#if UNITY_EDITOR
    [SerializeField] private float _editorOfflineCountdownSeconds = 5f;
#endif

    private int _lastCountdownSecond = -1;
    private Coroutine _countdownAnimCoroutine;
    private RectTransform _playerListContent;
    private bool _fallbackIssuedLoadingForThisCountdown;
#if UNITY_EDITOR
    private bool _editorOfflineCountdownActive;
    private float _editorOfflineCountdownEndTime;
#endif

    // Object Pooling for player entries
    private readonly Stack<GameObject> _playerEntryPool = new Stack<GameObject>();
    private readonly List<GameObject> _activePlayerEntries = new List<GameObject>();

    private void Awake()
    {
        _playerListContent = ResolvePlayerListContent();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    private void Start()
    {
        // 버튼 클릭 시 Ready 토글
        if (_readyButton != null)
            _readyButton.onClick.AddListener(OnReadyClicked);

        InitializeFallbackReadyState();
    }

    private void OnDestroy()
    {
        // 씬 전환 시 이벤트 정리
        if (_readyButton != null)
            _readyButton.onClick.RemoveListener(OnReadyClicked);
    }

    private void Update()
    {
        // 네트워크 상태/카운트다운을 매 프레임 UI에 반영
        UpdatePlayerCount();
        UpdateCountdown();
        UpdateReadyLabel();
        // NOTE: UpdatePlayerList() is event-driven via IInRoomCallbacks
    }

    private void OnReadyClicked()
    {
        // 룸 안에서만 Ready를 토글
        if (!PhotonNetwork.InRoom)
        {
#if UNITY_EDITOR
            StartEditorOfflineCountdown();
#endif
            return;
        }

        if (NetworkAuthorityManager.Instance != null)
        {
            NetworkAuthorityManager.Instance.ToggleReady();
            return;
        }

        ToggleReadyWithoutAuthorityManager();
    }

    private void UpdatePlayerCount()
    {
        if (_textPlayerCount == null)
            return;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            _textPlayerCount.text = string.Empty;
            return;
        }

        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        _textPlayerCount.text = max > 0 ? $"{current}/{max}" : $"{current}";
    }

    private void UpdatePlayerList()
    {
        RectTransform content = _playerListContent != null ? _playerListContent : ResolvePlayerListContent();
        if (content == null)
            return;

        // Return currently active entries to pool
        foreach (GameObject entry in _activePlayerEntries)
        {
            if (entry != null)
            {
                entry.SetActive(false);
                _playerEntryPool.Push(entry);
            }
        }
        _activePlayerEntries.Clear();

        if (!PhotonNetwork.InRoom)
            return;

        Player[] players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        for (int i = 0; i < players.Length; i++)
        {
            string name = string.IsNullOrWhiteSpace(players[i].NickName) ? $"Player_{players[i].ActorNumber}" : players[i].NickName;

            GameObject entry = GetPooledEntry(content);

            Text entryText = entry.GetComponentInChildren<Text>();
            if (entryText != null)
            {
                entryText.text = name;

                // Ready 상태에 따라 텍스트 색상 변경
                bool isReady = false;
                if (players[i].CustomProperties != null &&
                    players[i].CustomProperties.TryGetValue(PlayerPropReady, out object raw) &&
                    raw is bool b)
                {
                    isReady = b;
                }
                entryText.color = isReady ? Color.green : Color.white;
            }

            _activePlayerEntries.Add(entry);
        }
    }

    private GameObject GetPooledEntry(RectTransform content)
    {
        // Try pool first
        while (_playerEntryPool.Count > 0)
        {
            GameObject pooled = _playerEntryPool.Pop();
            if (pooled != null)
            {
                pooled.SetActive(true);
                return pooled;
            }
        }

        // Instantiate new entry
        GameObject entry;
        if (_playerEntryTemplate != null)
        {
            entry = Instantiate(_playerEntryTemplate, content);
            entry.SetActive(true);
        }
        else
        {
            entry = new GameObject("PlayerEntry");
            entry.transform.SetParent(content, false);
        }
        return entry;
    }

    private void UpdateCountdown()
    {
        if (_textCountdown == null)
            return;

#if UNITY_EDITOR
        if (!PhotonNetwork.InRoom && _editorOfflineCountdownActive)
        {
            float remainingOffline = _editorOfflineCountdownEndTime - Time.time;
            if (remainingOffline > 0f)
            {
                SetCountdownVisible(true);
                int offlineSecondsLeft = Mathf.Clamp(Mathf.CeilToInt(remainingOffline), 0, 999);
                if (offlineSecondsLeft != _lastCountdownSecond)
                {
                    _lastCountdownSecond = offlineSecondsLeft;
                    _textCountdown.text = offlineSecondsLeft.ToString();

                    if (_countdownAnimCoroutine != null)
                        StopCoroutine(_countdownAnimCoroutine);

                    _countdownAnimCoroutine = StartCoroutine(PlayCountdownPop());
                }

                return;
            }

            _editorOfflineCountdownActive = false;
        }

        if (PhotonNetwork.InRoom)
            _editorOfflineCountdownActive = false;
#endif

        if (!PhotonNetwork.InRoom)
        {
            SetCountdownVisible(false);
            return;
        }

        if (NetworkAuthorityManager.Instance == null)
            EvaluateFallbackCountdown();

        if (!TryGetCountdownRemaining(out float remaining, out bool isActive))
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
            _textCountdown.text = secondsLeft.ToString();

            if (_countdownAnimCoroutine != null)
                StopCoroutine(_countdownAnimCoroutine);

            _countdownAnimCoroutine = StartCoroutine(PlayCountdownPop());
        }

        HandleFallbackCountdownCompletion(remaining);
    }

    private void SetCountdownVisible(bool isVisible)
    {
        if (_textCountdown == null)
            return;

        if (_hideCountdownWhenInactive && _textCountdown.gameObject.activeSelf != isVisible)
            _textCountdown.gameObject.SetActive(isVisible);

        if (!isVisible)
        {
            _textCountdown.text = string.Empty;
            _lastCountdownSecond = -1;

            if (_countdownAnimCoroutine != null)
            {
                StopCoroutine(_countdownAnimCoroutine);
                _countdownAnimCoroutine = null;
            }

            RectTransform rt = _textCountdown.rectTransform;
            if (rt != null)
                rt.localScale = Vector3.one;
        }
    }

    private IEnumerator PlayCountdownPop()
    {
        if (_textCountdown == null)
            yield break;

        RectTransform rt = _textCountdown.rectTransform;
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
        if (_textReady == null)
            return;

        if (!PhotonNetwork.InRoom)
        {
            _textReady.text = "Ready";
            return;
        }

        bool isReady = false;
        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerPropReady, out object raw) &&
            raw is bool b)
        {
            isReady = b;
        }

        _textReady.text = isReady ? "Unready" : "Ready";
    }

    private RectTransform ResolvePlayerListContent()
    {
        if (_playerListScrollView == null)
            return null;

        ScrollRect scrollRect = _playerListScrollView.GetComponent<ScrollRect>();
        if (scrollRect != null)
            return scrollRect.content;

        return _playerListScrollView;
    }

    private void ToggleReadyWithoutAuthorityManager()
    {
        bool current = false;
        if (PhotonNetwork.LocalPlayer != null &&
            PhotonNetwork.LocalPlayer.CustomProperties != null &&
            PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(PlayerPropReady, out object raw) &&
            raw is bool b)
        {
            current = b;
        }

        PhotonHashtable props = new PhotonHashtable
        {
            { PlayerPropReady, !current }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void InitializeFallbackReadyState()
    {
        if (NetworkAuthorityManager.Instance != null || !PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return;

        PhotonHashtable props = new PhotonHashtable
        {
            { PlayerPropReady, false }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void EvaluateFallbackCountdown()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        if (!AreAllPlayersReadyWithoutAuthorityManager())
        {
            StopFallbackCountdown();
            return;
        }

        StartFallbackCountdownIfNeeded();
    }

    private bool AreAllPlayersReadyWithoutAuthorityManager()
    {
        if (!PhotonNetwork.InRoom)
            return false;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties == null ||
                !player.CustomProperties.TryGetValue(PlayerPropReady, out object raw) ||
                raw is not bool ready ||
                !ready)
            {
                return false;
            }
        }

        return true;
    }

    private void StartFallbackCountdownIfNeeded()
    {
        if (IsFallbackCountdownActive())
            return;

        _fallbackIssuedLoadingForThisCountdown = false;

        PhotonHashtable props = new PhotonHashtable
        {
            { RoomPropCountdownActive, true },
            { RoomPropCountdownStartTime, PhotonNetwork.Time },
            { RoomPropCountdownDuration, _fallbackCountdownSeconds }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void StopFallbackCountdown()
    {
        if (!IsFallbackCountdownActive() || PhotonNetwork.CurrentRoom == null)
            return;

        _fallbackIssuedLoadingForThisCountdown = false;

        PhotonHashtable props = new PhotonHashtable
        {
            { RoomPropCountdownActive, false }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private bool IsFallbackCountdownActive()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties == null)
            return false;

        return PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPropCountdownActive, out object raw) &&
               raw is bool active &&
               active;
    }

    private bool TryGetCountdownRemaining(out float remainingSeconds, out bool isActive)
    {
        if (NetworkAuthorityManager.Instance != null)
            return NetworkAuthorityManager.Instance.TryGetLobbyCountdownRemaining(out remainingSeconds, out isActive);

        remainingSeconds = 0f;
        isActive = false;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.CustomProperties == null)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPropCountdownActive, out object activeRaw) || activeRaw is not bool active)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPropCountdownStartTime, out object startRaw) || startRaw is not double startTime)
            return false;

        if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(RoomPropCountdownDuration, out object durationRaw))
            return false;

        float durationSeconds = durationRaw switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            _ => 0f
        };

        isActive = active;
        if (!isActive)
            return true;

        remainingSeconds = Mathf.Max(0f, durationSeconds - (float)(PhotonNetwork.Time - startTime));
        return true;
    }

    private void HandleFallbackCountdownCompletion(float remainingSeconds)
    {
        if (NetworkAuthorityManager.Instance != null ||
            !PhotonNetwork.InRoom ||
            !PhotonNetwork.IsMasterClient ||
            PhotonNetwork.CurrentRoom == null ||
            _fallbackIssuedLoadingForThisCountdown ||
            remainingSeconds > 0f)
        {
            return;
        }

        if (!AreAllPlayersReadyWithoutAuthorityManager())
        {
            StopFallbackCountdown();
            return;
        }

        _fallbackIssuedLoadingForThisCountdown = true;
        PhotonNetwork.CurrentRoom.IsOpen = false;

        if (!string.IsNullOrWhiteSpace(_fallbackLoadingSceneName))
            PhotonNetwork.LoadLevel(_fallbackLoadingSceneName);
    }

#if UNITY_EDITOR
    private void StartEditorOfflineCountdown()
    {
        _editorOfflineCountdownActive = true;
        _editorOfflineCountdownEndTime = Time.time + Mathf.Max(0.1f, _editorOfflineCountdownSeconds);
        _lastCountdownSecond = -1;
    }
#endif

    // IInRoomCallbacks implementation — rebuild player list on Photon events
    public void OnPlayerEnteredRoom(Player newPlayer) { UpdatePlayerList(); }
    public void OnPlayerLeftRoom(Player otherPlayer) { UpdatePlayerList(); }
    public void OnPlayerPropertiesUpdate(Player target, PhotonHashtable changedProps)
    {
        if (changedProps.ContainsKey(PlayerPropReady))
            UpdatePlayerList();
    }
    public void OnRoomPropertiesUpdate(PhotonHashtable propertiesThatChanged) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }
}
