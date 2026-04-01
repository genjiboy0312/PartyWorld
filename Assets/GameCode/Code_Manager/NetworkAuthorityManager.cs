using System.Collections;
using System.Collections.Generic;
using System.IO;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkAuthorityManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    private struct CharacterSpawnEntry
    {
        public string characterId;
        public string prefabName;
    }

    public static NetworkAuthorityManager Instance { get; private set; }

    private const string DEBUG_PREF_AUTO_QUICKPLAY = "PW_DEBUG_AUTO_QUICKPLAY";
    private const string DEBUG_PREF_FORCE_NICKNAME = "PW_DEBUG_FORCE_NICKNAME";
    private const string PREF_SELECTED_CHARACTER_ID = "PW_SELECTED_CHARACTER_ID";
    private const string PREF_SELECTED_CHARACTER_PREFAB = "PW_SELECTED_CHARACTER_PREFAB";
    private const string CHARACTER_CATALOG_RESOURCE_PATH = "CharacterCatalog";

    private const string PLAYER_PROP_READY = "ready";
    private const string PLAYER_PROP_SCENE = "scene";
    private const string PLAYER_PROP_CHARACTER_ID = "characterId";
    private const string ROOM_PROP_SELECTED_MAP = "selectedMap";
    private const string ROOM_PROP_COUNTDOWN_ACTIVE = "lobbyCountdownActive";
    private const string ROOM_PROP_COUNTDOWN_START_TIME = "lobbyCountdownStartTime";
    private const string ROOM_PROP_COUNTDOWN_DURATION = "lobbyCountdownDuration";

    [Header("Connection")]
    [SerializeField] private bool _autoConnect = false;
    [SerializeField] private bool _autoMatchmake = false;
    [SerializeField] private string _gameVersion = "1.0f";

    [Header("Room Options")]
    [SerializeField] private byte _maxPlayers = 8;
    [SerializeField] private bool _isRoomOpen = true;
    [SerializeField] private bool _isRoomVisible = true;

    [Header("Scenes")]
    [SerializeField] private SceneReference _titleScene = new SceneReference();
    [SerializeField] private SceneReference _waitingRoomScene = new SceneReference();
    [SerializeField] private SceneReference _roomLobbyScene = new SceneReference();
    [SerializeField] private SceneReference _loadingScene = new SceneReference();
    [SerializeField] private List<SceneReference> _playMapScenes = new List<SceneReference>();

    // 런타임용 문자열 캐시
    private string _titleSceneName;
    private string _waitingRoomSceneName;
    private string _roomLobbySceneName;
    private string _loadingSceneName;

    [Header("Character Spawn")]
    [SerializeField] private CharacterCatalog _characterCatalog;
    [SerializeField] private List<CharacterSpawnEntry> _characterSpawnEntries = new List<CharacterSpawnEntry>();
    [SerializeField] private string _defaultCharacterId = "bear_base";
    [SerializeField] private string _defaultPlayerPrefabName = "Player_Test01";
    [SerializeField] private Vector3 _fallbackSpawnPosition = Vector3.zero;
    [SerializeField] private Vector3 _fallbackSpawnStep = new Vector3(1.5f, 0f, 0f);

    [Header("Networking")]
    [SerializeField] private int _sendRate = 60;
    [SerializeField] private int _serializationRate = 60;
    [SerializeField] private float _loadingGateDelaySeconds = 0.25f;

    [Header("Lobby Countdown")]
    [SerializeField] private float _lobbyCountdownSeconds = 5f;

    [Header("Debug/Retry")]
    [SerializeField] private float _joinTimeoutSeconds = 10f;
    [SerializeField] private float _retryDelaySeconds = 1.5f;

    private Coroutine _loadingGateCoroutine;
    private bool _quickPlayRequested;
    private bool _issuedLoadingForThisCountdown;
    private Coroutine _quickPlayWatchCoroutine;
    private GameObject _localSpawnedPlayer;
    public GameObject LocalSpawnedPlayer => _localSpawnedPlayer;
    private bool _characterCatalogTriedLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // 씬에 미배치된 경우에도 네트워크 권위 매니저를 1개만 보장
        if (Object.FindAnyObjectByType<NetworkAuthorityManager>() != null)
            return;

        GameObject go = new GameObject(nameof(NetworkAuthorityManager));
        go.AddComponent<NetworkAuthorityManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = _gameVersion;

        if (_sendRate > 0)
            PhotonNetwork.SendRate = _sendRate;
        if (_serializationRate > 0)
            PhotonNetwork.SerializationRate = _serializationRate;

        // SceneReference에서 문자열 캐시 생성
        _titleSceneName = _titleScene?.SceneName ?? "";
        _waitingRoomSceneName = _waitingRoomScene?.SceneName ?? "";
        _roomLobbySceneName = _roomLobbyScene?.SceneName ?? "";
        _loadingSceneName = _loadingScene?.SceneName ?? "";
    }

    public override void OnEnable()
    {
        // PUN 콜백 등록(필수)
        base.OnEnable();

        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // PUN 콜백 해제
        base.OnDisable();
    }

    private void Start()
    {
        // 자동 연결 옵션이면 시작 시점에만 연결 시도(중복 호출 방지)
        if (_autoConnect)
            ConnectIfNeeded();

        // 디버그 옵션이면 파이어베이스 플로우 없이도 매칭을 시작
        TryStartDebugQuickPlay(SceneManager.GetActiveScene().name);
    }

    public void StartQuickPlay()
    {
        // 사용자가 명시적으로 QuickPlay를 눌렀을 때만 매칭을 시작
        if (_quickPlayRequested)
            return;

        _quickPlayRequested = true;

        ConnectIfNeeded();

        // WaitingRoom 등에서 채팅용 Room에 먼저 들어가 있을 수 있으므로, QuickPlay 시작 시 Room을 먼저 빠져나감
        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"[NetworkAuthorityManager] StartQuickPlay while in room -> LeaveRoom (room={PhotonNetwork.CurrentRoom?.Name})");
            PhotonNetwork.LeaveRoom();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
            TryJoinRandomOrCreateRoom();

        Debug.Log($"[NetworkAuthorityManager] StartQuickPlay (state={PhotonNetwork.NetworkClientState}, inRoom={PhotonNetwork.InRoom}, inLobby={PhotonNetwork.InLobby})");

        // 매칭이 멈추는 케이스를 대비해 타임아웃 감시
        if (_quickPlayWatchCoroutine == null)
            _quickPlayWatchCoroutine = StartCoroutine(WatchQuickPlay());
    }

    public override void OnLeftRoom()
    {
        // 채팅용 Room 등에서 빠져나온 뒤 QuickPlay 룸 매칭을 이어서 진행
        if (!_quickPlayRequested)
            return;

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
            TryJoinRandomOrCreateRoom();

        Debug.Log($"[NetworkAuthorityManager] OnLeftRoom (state={PhotonNetwork.NetworkClientState}, inRoom={PhotonNetwork.InRoom})");
    }

    public void ConnectIfNeeded()
    {
        // 이미 연결되어 있으면 아무것도 하지 않음(단일 책임 유지)
        if (PhotonNetwork.IsConnected)
            return;

        EnsureNickName();
        PhotonNetwork.ConnectUsingSettings();
    }

    public void SetReady(bool isReady)
    {
        // 룸 안에서만 Ready 상태를 의미있게 다룸
        if (!PhotonNetwork.InRoom)
            return;

        SetLocalPlayerProperty(PLAYER_PROP_READY, isReady);
    }

    public void ToggleReady()
    {
        bool current = GetBool(PhotonNetwork.LocalPlayer, PLAYER_PROP_READY, false);
        SetReady(!current);
    }

    public bool AreAllPlayersReady()
    {
        // 마스터가 “시작 가능 여부”를 판단할 때 쓰는 스냅샷 체크
        if (!PhotonNetwork.InRoom)
            return false;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!GetBool(p, PLAYER_PROP_READY, false))
                return false;
        }

        return true;
    }

    public bool TryGetLobbyCountdownRemaining(out float remainingSeconds, out bool isActive)
    {
        remainingSeconds = 0f;
        isActive = false;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return false;

        PhotonHashtable props = PhotonNetwork.CurrentRoom.CustomProperties as PhotonHashtable;
        if (props == null)
            return false;

        if (!props.TryGetValue(ROOM_PROP_COUNTDOWN_ACTIVE, out object activeRaw) || activeRaw is not bool active)
            return false;

        if (!props.TryGetValue(ROOM_PROP_COUNTDOWN_START_TIME, out object startRaw) || startRaw is not double startTime)
            return false;

        if (!props.TryGetValue(ROOM_PROP_COUNTDOWN_DURATION, out object durRaw))
            return false;

        float durationSeconds = durRaw switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            _ => 0f
        };

        isActive = active;
        if (!isActive)
            return true;

        double now = PhotonNetwork.Time;
        remainingSeconds = Mathf.Max(0f, durationSeconds - (float)(now - startTime));
        return true;
    }

    public void RequestStartMatch()
    {
        // 마스터만 로딩/맵 전환을 트리거(권위 단일화)
        if (!PhotonNetwork.InRoom)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        if (!AreAllPlayersReady())
            return;

        SelectAndStoreRandomMap();
        PhotonNetwork.LoadLevel(_loadingSceneName);
    }

    public override void OnConnectedToMaster()
    {
        // 자동 매칭 옵션이면 로비 진입 후 랜덤 매칭 시도
        if (!_autoMatchmake && !_quickPlayRequested)
            return;

        TryJoinRandomOrCreateRoom();
        Debug.Log($"[NetworkAuthorityManager] OnConnectedToMaster (state={PhotonNetwork.NetworkClientState}, inRoom={PhotonNetwork.InRoom}, inLobby={PhotonNetwork.InLobby})");
    }

    public override void OnJoinedLobby()
    {
        // 로비 기반 플로우는 사용하지 않음(JoinRandomOrCreateRoom 사용)
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // 랜덤 조인 실패 시 룸 생성으로 폴백(빠른 매칭)
        if (!_autoMatchmake && !_quickPlayRequested)
            return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = _maxPlayers,
            IsOpen = _isRoomOpen,
            IsVisible = _isRoomVisible
        };

        PhotonNetwork.CreateRoom(null, options);
        Debug.Log($"[NetworkAuthorityManager] OnJoinRandomFailed -> CreateRoom (code={returnCode}, msg={message})");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        // 조인 실패 시 상태를 로그로 남기고 재시도 대기
        Debug.LogWarning($"[NetworkAuthorityManager] OnJoinRoomFailed (code={returnCode}, msg={message}, state={PhotonNetwork.NetworkClientState})");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        // 생성 실패 시 상태를 로그로 남기고 재시도 대기
        Debug.LogWarning($"[NetworkAuthorityManager] OnCreateRoomFailed (code={returnCode}, msg={message}, state={PhotonNetwork.NetworkClientState})");
    }

    public override void OnJoinedRoom()
    {
        bool joinedForMatchmaking = _autoMatchmake || _quickPlayRequested;
        // 룸 입장 직후: Ready/Scene 프로퍼티를 초기화(동기화 게이트 기반)
        SetLocalPlayerProperty(PLAYER_PROP_READY, false);
        SetLocalPlayerProperty(PLAYER_PROP_SCENE, SceneManager.GetActiveScene().name);

        string selectedCharacterId = ResolveSelectedCharacterId();
        if (!string.IsNullOrWhiteSpace(selectedCharacterId))
            SetLocalPlayerProperty(PLAYER_PROP_CHARACTER_ID, selectedCharacterId);

        // 룸에 들어오면 “룸 로비” 씬으로 통일(전원 같은 공간에서 Ready/미니게임)
        if (joinedForMatchmaking && SceneManager.GetActiveScene().name != _roomLobbySceneName)
            PhotonNetwork.LoadLevel(_roomLobbySceneName);

        // 매칭 요청은 1회성으로 처리
        _quickPlayRequested = false;

        if (joinedForMatchmaking)
            Debug.Log($"[NetworkAuthorityManager] OnJoinedRoom -> LoadLevel({_roomLobbySceneName}) (room={PhotonNetwork.CurrentRoom?.Name})");
        else
            Debug.Log($"[NetworkAuthorityManager] OnJoinedRoom (no scene load) (room={PhotonNetwork.CurrentRoom?.Name})");

        _localSpawnedPlayer = null;

        // 매칭 감시 코루틴 정리
        StopQuickPlayWatch();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // 끊김 원인을 로그로 남김(디버그에 중요)
        Debug.LogWarning($"[NetworkAuthorityManager] OnDisconnected (cause={cause}, state={PhotonNetwork.NetworkClientState})");
    }

    private void TryJoinRandomOrCreateRoom()
    {
        // ConnectedToMaster 상태에서 랜덤 룸 조인, 실패 시 생성까지 한번에 처리
        ClientState state = PhotonNetwork.NetworkClientState;

        // 이미 조인/생성 관련 오퍼레이션이 진행 중이면 중복 호출하지 않음
        if (state == ClientState.Joining || state == ClientState.ConnectingToGameServer || state == ClientState.Leaving)
            return;

        if (state != ClientState.ConnectedToMasterServer && state != ClientState.ConnectedToMaster)
            return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = _maxPlayers,
            IsOpen = _isRoomOpen,
            IsVisible = _isRoomVisible
        };

        PhotonNetwork.JoinRandomOrCreateRoom(roomOptions: options);
    }

    private IEnumerator WatchQuickPlay()
    {
        // 일정 시간 내에 룸에 못 들어가면 끊고 재시도(디버그용 안정화)
        float startTime = Time.realtimeSinceStartup;

        while (_quickPlayRequested && !PhotonNetwork.InRoom)
        {
            if (Time.realtimeSinceStartup - startTime > _joinTimeoutSeconds)
            {
                Debug.LogWarning($"[NetworkAuthorityManager] QuickPlay timeout -> retry (state={PhotonNetwork.NetworkClientState})");

                PhotonNetwork.Disconnect();
                yield return new WaitForSeconds(_retryDelaySeconds);

                _quickPlayWatchCoroutine = null;
                if (_quickPlayRequested)
                    StartQuickPlay();

                yield break;
            }

            yield return null;
        }

        StopQuickPlayWatch();
    }

    private void StopQuickPlayWatch()
    {
        // 중복 감시를 막기 위해 코루틴 1개만 유지
        if (_quickPlayWatchCoroutine == null)
            return;

        StopCoroutine(_quickPlayWatchCoroutine);
        _quickPlayWatchCoroutine = null;
    }

    private void EnsureNickName()
    {
        // 닉네임이 없으면 개발용 기본 닉네임을 세팅(채팅/유저리스트 안정화)
        if (!string.IsNullOrWhiteSpace(PhotonNetwork.NickName))
            return;

        if (DataManager.Instance != null &&
            !string.IsNullOrWhiteSpace(DataManager.Instance.CurrentUserData.nickname) &&
            DataManager.Instance.CurrentUserData.nickname != "NewPlayer")
        {
            PhotonNetwork.NickName = DataManager.Instance.CurrentUserData.nickname;
            return;
        }

        PhotonNetwork.NickName = $"Dev_{Random.Range(1000, 9999)}";
    }

    private string ResolveSelectedCharacterId()
    {
        if (DataManager.Instance != null &&
            DataManager.Instance.CurrentUserData != null &&
            !string.IsNullOrWhiteSpace(DataManager.Instance.CurrentUserData.selectedCharacterId))
        {
            return DataManager.Instance.CurrentUserData.selectedCharacterId;
        }

        string savedId = PlayerPrefs.GetString(PREF_SELECTED_CHARACTER_ID, string.Empty);
        if (!string.IsNullOrWhiteSpace(savedId))
            return savedId;

        return string.Empty;
    }

    private void TryStartDebugQuickPlay(string sceneName)
    {
        // 에디터/개발 빌드에서만 디버그 매칭을 허용
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이미 QuickPlay가 진행 중이면 중복 시작하지 않음
        if (_quickPlayRequested || _quickPlayWatchCoroutine != null)
            return;

        bool autoQuickPlay = PlayerPrefs.GetInt(DEBUG_PREF_AUTO_QUICKPLAY, 0) == 1;
        if (!autoQuickPlay)
            return;

        // WaitingRoom/룸 로비에서만 자동 매칭(맵/로딩에서의 재매칭 방지)
        if (sceneName != _waitingRoomSceneName && sceneName != _roomLobbySceneName)
            return;

        // 이미 룸이 있으면 아무것도 하지 않음
        if (PhotonNetwork.InRoom)
            return;

        // 디버그 닉네임 강제 옵션
        if (PlayerPrefs.GetInt(DEBUG_PREF_FORCE_NICKNAME, 0) == 1)
            PhotonNetwork.NickName = $"Dev_{System.Environment.UserName}";

        Debug.Log($"[NetworkAuthorityManager] Debug Auto QuickPlay -> StartQuickPlay (scene={sceneName})");
        StartQuickPlay();
#endif
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        // 로비 씬에서: Ready 변화에 따라 카운트다운 시작/취소(마스터만)
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == _roomLobbySceneName)
        {
            EvaluateLobbyCountdown();
            return;
        }

        // 로딩 씬에서: 모든 플레이어가 로딩 씬에 도착하면 맵으로 전환(마스터만)
        if (sceneName != _loadingSceneName)
            return;

        if (_loadingGateCoroutine == null && AreAllPlayersInScene(_loadingSceneName))
            _loadingGateCoroutine = StartCoroutine(LoadingGateToMap());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 도착을 플레이어 프로퍼티로 남겨서 “모두 도착” 게이트에 사용
        if (PhotonNetwork.InRoom)
            SetLocalPlayerProperty(PLAYER_PROP_SCENE, scene.name);

        SyncGameManagerState(scene.name);

        if (!PhotonNetwork.InRoom)
        {
            // 디버그 옵션이면 룸이 없어도 로비 씬에서 자동 매칭을 시작
            TryStartDebugQuickPlay(scene.name);
            return;
        }

        if (scene.name.StartsWith("Scene_Map"))
            TrySpawnLocalPlayerForMap();

        if (!PhotonNetwork.IsMasterClient)
            return;

        if (scene.name == _roomLobbySceneName)
        {
            EvaluateLobbyCountdown();
            return;
        }

        if (scene.name == _loadingSceneName)
        {
            if (_loadingGateCoroutine == null && AreAllPlayersInScene(_loadingSceneName))
                _loadingGateCoroutine = StartCoroutine(LoadingGateToMap());

            return;
        }
    }

    private IEnumerator LoadingGateToMap()
    {
        // 씬 로딩 직후 짧은 대기(프로퍼티/콜백 반영 시간 확보)
        yield return new WaitForSeconds(_loadingGateDelaySeconds);

        if (!PhotonNetwork.IsMasterClient)
        {
            _loadingGateCoroutine = null;
            yield break;
        }

        if (!AreAllPlayersInScene(_loadingSceneName))
        {
            _loadingGateCoroutine = null;
            yield break;
        }

        PhotonNetwork.LoadLevel(GetSelectedMapSceneName());
        _loadingGateCoroutine = null;
    }

    private void EvaluateLobbyCountdown()
    {
        // 룸 로비에서 전원 Ready면 카운트다운을 시작하고, 깨지면 즉시 취소
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        bool allReady = AreAllPlayersReady();

        if (!allReady)
        {
            StopLobbyCountdown();
            return;
        }

        StartLobbyCountdownIfNeeded();
    }

    private void StartLobbyCountdownIfNeeded()
    {
        // 이미 카운트다운이 켜져 있으면 중복 시작하지 않음
        if (IsLobbyCountdownActive())
            return;

        _issuedLoadingForThisCountdown = false;

        PhotonHashtable props = new PhotonHashtable
        {
            { ROOM_PROP_COUNTDOWN_ACTIVE, true },
            { ROOM_PROP_COUNTDOWN_START_TIME, PhotonNetwork.Time },
            { ROOM_PROP_COUNTDOWN_DURATION, _lobbyCountdownSeconds }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private void StopLobbyCountdown()
    {
        // 카운트다운을 끄고, 다음 Ready 조합에서 다시 시작 가능하게 리셋
        if (!IsLobbyCountdownActive())
            return;

        _issuedLoadingForThisCountdown = false;

        PhotonHashtable props = new PhotonHashtable
        {
            { ROOM_PROP_COUNTDOWN_ACTIVE, false }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private bool IsLobbyCountdownActive()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return false;

        PhotonHashtable props = PhotonNetwork.CurrentRoom.CustomProperties as PhotonHashtable;
        if (props == null)
            return false;

        return props.TryGetValue(ROOM_PROP_COUNTDOWN_ACTIVE, out object raw) && raw is bool active && active;
    }

    private void Update()
    {
        // 마스터는 카운트다운 만료 시 로딩 씬으로 전환(한 번만)
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (SceneManager.GetActiveScene().name != _roomLobbySceneName)
            return;

        if (_issuedLoadingForThisCountdown)
            return;

        if (!TryGetLobbyCountdownRemaining(out float remaining, out bool active))
            return;

        if (!active)
            return;

        if (remaining > 0f)
            return;

        if (!AreAllPlayersReady())
        {
            StopLobbyCountdown();
            return;
        }

        _issuedLoadingForThisCountdown = true;
        SelectAndStoreRandomMap();
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(_loadingSceneName);
    }

    private void SelectAndStoreRandomMap()
    {
        // 라운드 시작 직전에 맵을 1개 뽑아 룸 프로퍼티로 고정(전원 동일 맵 보장)
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        List<string> candidates = GetCandidateMapSceneNames();
        if (candidates.Count == 0)
        {
            Debug.LogError("[NetworkAuthorityManager] No maps configured in _playMapScenes!");
            return;
        }

        string selected = candidates[Random.Range(0, candidates.Count)];

        PhotonHashtable props = new PhotonHashtable
        {
            { ROOM_PROP_SELECTED_MAP, selected }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private string GetSelectedMapSceneName()
    {
        // 룸 프로퍼티 기반으로 선택된 맵을 읽어옴(없으면 첫 번째 맵)
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            List<string> candidates = GetCandidateMapSceneNames();
            return candidates.Count > 0 ? candidates[0] : string.Empty;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ROOM_PROP_SELECTED_MAP, out object raw) &&
            raw is string sceneName &&
            !string.IsNullOrWhiteSpace(sceneName))
        {
            return sceneName;
        }

        // 룸 프로퍼티에 없으면 첫 번째 맵 반환
        List<string> fallbackCandidates = GetCandidateMapSceneNames();
        return fallbackCandidates.Count > 0 ? fallbackCandidates[0] : string.Empty;
    }

    private List<string> GetCandidateMapSceneNames()
    {
        // 인스펙터에서 지정한 맵 리스트만 사용 (SceneReference)
        List<string> candidates = new List<string>();

        if (_playMapScenes != null)
        {
            for (int i = 0; i < _playMapScenes.Count; i++)
            {
                string sceneName = _playMapScenes[i]?.SceneName;
                if (!string.IsNullOrWhiteSpace(sceneName))
                    candidates.Add(sceneName);
            }
        }

        return candidates;
    }

    private bool AreAllPlayersInScene(string sceneName)
    {
        // 로딩 게이트: 모든 플레이어의 scene 프로퍼티가 목표 씬인지 확인
        if (!PhotonNetwork.InRoom)
            return false;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            string playerScene = GetString(p, PLAYER_PROP_SCENE, string.Empty);
            if (playerScene != sceneName)
                return false;
        }

        return true;
    }

    private void SetLocalPlayerProperty(string key, object value)
    {
        // CustomProperties는 부분 업데이트(키 단위)로만 갱신
        if (PhotonNetwork.LocalPlayer == null)
            return;

        PhotonHashtable props = new PhotonHashtable { { key, value } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private static bool GetBool(Player player, string key, bool defaultValue)
    {
        if (player == null || player.CustomProperties == null)
            return defaultValue;

        if (!player.CustomProperties.TryGetValue(key, out object raw))
            return defaultValue;

        return raw is bool b ? b : defaultValue;
    }

    private static string GetString(Player player, string key, string defaultValue)
    {
        if (player == null || player.CustomProperties == null)
            return defaultValue;

        if (!player.CustomProperties.TryGetValue(key, out object raw))
            return defaultValue;

        return raw as string ?? defaultValue;
    }

    private void SyncGameManagerState(string sceneName)
    {
        // 씬 상태 매핑은 GameManager가 단일 책임으로 관리
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        gm.SyncStateByScene(sceneName);
    }

    private void TrySpawnLocalPlayerForMap()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return;

        Vector3 spawnPosition = _fallbackSpawnPosition;
        spawnPosition += _fallbackSpawnStep * (PhotonNetwork.LocalPlayer.ActorNumber - 1);
        SpawnLocalSelectedCharacter(spawnPosition, Quaternion.identity, true);
    }

    public GameObject SpawnLocalSelectedCharacter(Vector3 position, Quaternion rotation, bool forceRespawn)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return null;

        if (_localSpawnedPlayer != null)
        {
            if (!forceRespawn)
                return _localSpawnedPlayer;

            PhotonView view = _localSpawnedPlayer.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
                PhotonNetwork.Destroy(_localSpawnedPlayer);

            _localSpawnedPlayer = null;
        }

        string characterId = ResolveSelectedCharacterId();
        string prefabName = ResolvePlayerPrefabName(characterId);
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            Debug.LogWarning("[NetworkAuthorityManager] prefabName is empty. check character spawn entries.");
            return null;
        }

        string instantiateKey = ResolvePhotonPrefabKey(prefabName);
        if (string.IsNullOrWhiteSpace(instantiateKey))
        {
            Debug.LogWarning($"[NetworkAuthorityManager] Photon prefab not found in Resources. prefabName={prefabName}");
            return null;
        }

        try
        {
            _localSpawnedPlayer = PhotonNetwork.Instantiate(instantiateKey, position, rotation);
            Debug.Log($"[NetworkAuthorityManager] Spawned player prefab={instantiateKey}, characterId={characterId}");
            return _localSpawnedPlayer;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NetworkAuthorityManager] Spawn failed for prefab '{instantiateKey}': {e.Message}");
            return null;
        }
    }

    private string ResolvePlayerPrefabName(string characterId)
    {
        EnsureCharacterCatalogLoaded();

        if (_characterCatalog != null)
        {
            GameObject catalogPrefab = _characterCatalog.GetPrefabOrDefault(characterId, _defaultCharacterId);
            if (catalogPrefab != null)
                return catalogPrefab.name;
        }

        if (!string.IsNullOrWhiteSpace(characterId) && _characterSpawnEntries != null)
        {
            for (int i = 0; i < _characterSpawnEntries.Count; i++)
            {
                CharacterSpawnEntry entry = _characterSpawnEntries[i];
                if (string.Equals(entry.characterId, characterId, System.StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(entry.prefabName))
                    return entry.prefabName;
            }
        }

        if (!string.IsNullOrWhiteSpace(_defaultCharacterId) && _characterSpawnEntries != null)
        {
            for (int i = 0; i < _characterSpawnEntries.Count; i++)
            {
                CharacterSpawnEntry entry = _characterSpawnEntries[i];
                if (string.Equals(entry.characterId, _defaultCharacterId, System.StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(entry.prefabName))
                    return entry.prefabName;
            }
        }

        string savedPrefab = PlayerPrefs.GetString(PREF_SELECTED_CHARACTER_PREFAB, string.Empty);
        if (!string.IsNullOrWhiteSpace(savedPrefab))
            return savedPrefab;

        return _defaultPlayerPrefabName;
    }

    private void EnsureCharacterCatalogLoaded()
    {
        if (_characterCatalog != null || _characterCatalogTriedLoad)
            return;

        _characterCatalogTriedLoad = true;
        _characterCatalog = Resources.Load<CharacterCatalog>(CHARACTER_CATALOG_RESOURCE_PATH);
        if (_characterCatalog == null)
            Debug.LogWarning("[NetworkAuthorityManager] CharacterCatalog not found in Resources/CharacterCatalog.");
    }

    private static string ResolvePhotonPrefabKey(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
            return string.Empty;

        if (Resources.Load<GameObject>(prefabName) != null)
            return prefabName;

        string keyInCharacters = $"Characters/{prefabName}";
        if (Resources.Load<GameObject>(keyInCharacters) != null)
            return keyInCharacters;

        string keyInPrefabsCharacters = $"Prefabs/Characters/{prefabName}";
        if (Resources.Load<GameObject>(keyInPrefabsCharacters) != null)
            return keyInPrefabsCharacters;

        return string.Empty;
    }

    public void ReturnToLobby()
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (_localSpawnedPlayer != null)
        {
            PhotonView view = _localSpawnedPlayer.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
                PhotonNetwork.Destroy(_localSpawnedPlayer);

            _localSpawnedPlayer = null;
        }

        PhotonNetwork.LoadLevel(_roomLobbySceneName);
    }
}
