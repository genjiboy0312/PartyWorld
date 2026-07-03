using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkAuthorityManager : MonoBehaviourPunCallbacks
{
    public static NetworkAuthorityManager Instance { get; private set; }

    private const string DEBUG_PREF_AUTO_QUICKPLAY = "PW_DEBUG_AUTO_QUICKPLAY";
    private const string DEBUG_PREF_FORCE_NICKNAME = "PW_DEBUG_FORCE_NICKNAME";
    private const string PLAYER_PROP_READY = "ready";

    [Header("Connection")]
    [SerializeField] private bool _autoConnect = false;
    [SerializeField] private bool _autoMatchmake = false;
    [SerializeField] private string _gameVersion = "1.0f";

    [Header("Room Options")]
    [SerializeField] private byte _maxPlayers = 8;
    [SerializeField] private bool _isRoomOpen = true;
    [SerializeField] private bool _isRoomVisible = true;

    [SerializeField, HideInInspector] private SceneReference _titleScene = new SceneReference();
    [SerializeField, HideInInspector] private SceneReference _waitingRoomScene = new SceneReference();
    [SerializeField, HideInInspector] private SceneReference _roomLobbyScene = new SceneReference();
    [SerializeField, HideInInspector] private SceneReference _loadingScene = new SceneReference();
    [SerializeField, HideInInspector] private List<SceneReference> _playMapScenes = new List<SceneReference>();
    [SerializeField, HideInInspector] private string _mapScenePrefix = "Scene_Map";

    [SerializeField, HideInInspector] private CharacterCatalog _characterCatalog;
    [SerializeField, HideInInspector] private List<CharacterSpawnEntry> _characterSpawnEntries = new List<CharacterSpawnEntry>();
    [SerializeField, HideInInspector] private string _defaultCharacterId = "bear_base";
    [SerializeField, HideInInspector] private string _defaultPlayerPrefabName = "Player_Test01";
    [SerializeField, HideInInspector] private Vector3 _fallbackSpawnPosition = Vector3.zero;
    [SerializeField, HideInInspector] private Vector3 _fallbackSpawnStep = new Vector3(1.5f, 0f, 0f);

    [Header("Networking")]
    [SerializeField] private int _sendRate = 20;
    [SerializeField] private int _serializationRate = 20;
    [SerializeField, HideInInspector] private float _loadingGateDelaySeconds = 0.25f;

    [SerializeField, HideInInspector] private float _lobbyCountdownSeconds = 5f;

    [Header("Debug/Retry")]
    [SerializeField] private float _joinTimeoutSeconds = 10f;
    [SerializeField] private float _retryDelaySeconds = 1.5f;

    private LobbyCountdownManager _lobbyCountdown;
    private SceneTransitionManager _sceneTransition;
    private CharacterSpawnManager _characterSpawn;

    private bool _quickPlayRequested;
    private bool _createRoomRequested;
    private Coroutine _quickPlayWatchCoroutine;

    public GameObject LocalSpawnedPlayer => _characterSpawn != null ? _characterSpawn.LocalSpawnedPlayer : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureRoundManager();

        _lobbyCountdown = gameObject.GetOrAddComponent<LobbyCountdownManager>();
        _sceneTransition = gameObject.GetOrAddComponent<SceneTransitionManager>();
        _characterSpawn = gameObject.GetOrAddComponent<CharacterSpawnManager>();

        _characterSpawn.Configure(
            _characterCatalog,
            _characterSpawnEntries,
            _defaultCharacterId,
            _defaultPlayerPrefabName,
            _fallbackSpawnPosition,
            _fallbackSpawnStep);

        _sceneTransition.Configure(
            this,
            _characterSpawn,
            _titleScene,
            _waitingRoomScene,
            _roomLobbyScene,
            _loadingScene,
            _playMapScenes,
            _mapScenePrefix,
            _loadingGateDelaySeconds);

        _lobbyCountdown.Configure(this, _sceneTransition, _lobbyCountdownSeconds);

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = _gameVersion;

        if (_sendRate > 0)
            PhotonNetwork.SendRate = _sendRate;
        if (_serializationRate > 0)
            PhotonNetwork.SerializationRate = _serializationRate;
    }

    private void EnsureRoundManager()
    {
        if (RoundManager.Instance != null || GetComponent<RoundManager>() != null)
            return;

        gameObject.AddComponent<RoundManager>();
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

    public void RequestCreateRoom(string roomName, RoomOptions options)
    {
        _createRoomRequested = true;
        ConnectIfNeeded();
        PhotonNetwork.CreateRoom(roomName, options);
        Debug.Log($"[NetworkAuthorityManager] RequestCreateRoom(roomName={roomName})");
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
        else if (PhotonNetwork.InLobby)
        {
            // 로비에 있으면 TryJoinRandomOrCreateRoom가 동작하지 않으므로 먼저 로비를 나감
            Debug.Log("[NetworkAuthorityManager] StartQuickPlay while in lobby -> LeaveLobby");
            PhotonNetwork.LeaveLobby();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
            TryQuickPlayJoin();

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
            TryQuickPlayJoin();

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
        if (_lobbyCountdown != null)
            return _lobbyCountdown.TryGetLobbyCountdownRemaining(out remainingSeconds, out isActive);

        remainingSeconds = 0f;
        isActive = false;
        return false;
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

        _sceneTransition?.RequestStartMatch();
    }

    public GameObject SpawnLocalSelectedCharacter(Vector3 position, Quaternion rotation, bool forceRespawn)
    {
        return _characterSpawn != null
            ? _characterSpawn.SpawnLocalSelectedCharacter(position, rotation, forceRespawn)
            : null;
    }

    public void ReturnToLobby()
    {
        if (!PhotonNetwork.InRoom)
            return;

        _characterSpawn?.CleanupLocalSpawnedPlayer();
        _sceneTransition?.ReturnToLobby();
    }

    public override void OnConnectedToMaster()
    {
        // 자동 매칭 옵션이면 로비 진입 후 랜덤 매칭 시도
        if (!_autoMatchmake && !_quickPlayRequested)
            return;

        if (_quickPlayRequested)
            TryQuickPlayJoin();
        else
            TryJoinRandomOrCreateRoom();
        Debug.Log($"[NetworkAuthorityManager] OnConnectedToMaster (state={PhotonNetwork.NetworkClientState}, inRoom={PhotonNetwork.InRoom}, inLobby={PhotonNetwork.InLobby})");
    }

    public override void OnJoinedLobby()
    {
        // 로비 기반 플로우는 사용하지 않음(JoinRandomOrCreateRoom 사용)
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (!_autoMatchmake && !_quickPlayRequested)
            return;

        if (_quickPlayRequested)
        {
            // QuickPlay는 방 생성 없이 실패 처리
            Debug.Log($"[NetworkAuthorityManager] OnJoinRandomFailed (quick play - no room available): {message}");
            _quickPlayRequested = false;
            StopQuickPlayWatch();
            return;
        }

        // _autoMatchmake: 랜덤 조인 실패 시 룸 생성으로 폴백
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
        // 생성 실패 시 상태를 로그로 남기고 _createRoomRequested 초기화
        _createRoomRequested = false;
        Debug.LogWarning($"[NetworkAuthorityManager] OnCreateRoomFailed (code={returnCode}, msg={message}, state={PhotonNetwork.NetworkClientState})");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[NetworkAuthorityManager] OnJoinedRoom ENTER (autoMatchmake={_autoMatchmake}, quickPlayRequested={_quickPlayRequested}, createRoomRequested={_createRoomRequested}, scene={SceneManager.GetActiveScene().name})");
        bool joinedForMatchmaking = _autoMatchmake || _quickPlayRequested;
        bool shouldLoadWaitingRoom = joinedForMatchmaking || _createRoomRequested;

        // 룸 입장 직후: Ready/Scene 프로퍼티를 초기화(동기화 게이트 기반)
        SetLocalPlayerProperty(PLAYER_PROP_READY, false);
        _sceneTransition?.MarkLocalPlayerScene(SceneManager.GetActiveScene().name);
        _characterSpawn?.SyncLocalSelectedCharacterProperty(this);

        // 룸에 들어오면 WaitingRoom 씬으로 통일(전원 같은 공간에서 Ready/미니게임)
        _sceneTransition?.LoadWaitingRoomIfNeeded(shouldLoadWaitingRoom);

        // 매칭 요청은 1회성으로 처리
        _quickPlayRequested = false;
        _createRoomRequested = false;
        if (shouldLoadWaitingRoom)
            Debug.Log($"[NetworkAuthorityManager] OnJoinedRoom -> LoadLevel({_sceneTransition?.WaitingRoomSceneName}) (room={PhotonNetwork.CurrentRoom?.Name})");
        else
            Debug.Log($"[NetworkAuthorityManager] OnJoinedRoom (no scene load) (room={PhotonNetwork.CurrentRoom?.Name})");

        _characterSpawn?.ResetLocalSpawnedPlayerReference();

        // 매칭 감시 코루틴 정리
        StopQuickPlayWatch();
    }

    public override void OnLeftLobby()
    {
        // QuickPlay 요청 중 로비를 나간 후 ConnectedToMaster 상태에서 매칭 시작
        if (_quickPlayRequested && PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom)
            TryQuickPlayJoin();
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

        if (state != ClientState.ConnectedToMasterServer)
            return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = _maxPlayers,
            IsOpen = _isRoomOpen,
            IsVisible = _isRoomVisible
        };

        PhotonNetwork.JoinRandomOrCreateRoom(roomOptions: options);
    }

    private void TryQuickPlayJoin()
    {
        ClientState state = PhotonNetwork.NetworkClientState;

        if (state == ClientState.Joining || state == ClientState.ConnectingToGameServer || state == ClientState.Leaving)
            return;
        if (state != ClientState.ConnectedToMasterServer)
            return;

        // 빠른 매칭: 생성된 방 중 랜덤으로만 입장, 방이 없으면 실패
        PhotonNetwork.JoinRandomRoom();
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

        PhotonNetwork.NickName = $"Dev_{UnityEngine.Random.Range(1000, 9999)}";
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
        if (_sceneTransition == null ||
            (sceneName != _sceneTransition.WaitingRoomSceneName && sceneName != _sceneTransition.RoomLobbySceneName))
            return;

        // 이미 룸이 있으면 아무것도 하지 않음
        if (PhotonNetwork.InRoom)
            return;

        // 디버그 닉네임 강제 옵션
        if (PlayerPrefs.GetInt(DEBUG_PREF_FORCE_NICKNAME, 0) == 1)
            PhotonNetwork.NickName = $"Dev_{Environment.UserName}";

        Debug.Log($"[NetworkAuthorityManager] Debug Auto QuickPlay -> StartQuickPlay (scene={sceneName})");
        StartQuickPlay();
#endif
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);

        Debug.Log($"[NetworkAuthorityManager] Master switched to {newMasterClient.NickName} (Actor={newMasterClient.ActorNumber})");

        if (!PhotonNetwork.InRoom || _sceneTransition == null)
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        // 로비/웨이팅룸: 전체 Ready 상태에 따라 카운트다운 재평가
        if (_sceneTransition.IsLobbyScene(sceneName))
        {
            _lobbyCountdown?.EvaluateLobbyCountdown();
            return;
        }

        // 로딩 게이트: 새 마스터가 모든 플레이어 도착 확인 후 맵 전환
        if (_sceneTransition.IsLoadingScene(sceneName))
            _sceneTransition.TryStartLoadingGateIfReady();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        // 로비 씬에서: Ready 변화에 따라 카운트다운 시작/취소(마스터만)
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || _sceneTransition == null)
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        // 로비/웨이팅룸 씬에서: Ready 변화에 따라 카운트다운 시작/취소(마스터만)
        if (_sceneTransition.IsLobbyScene(sceneName))
        {
            _lobbyCountdown?.EvaluateLobbyCountdown();
            return;
        }

        // 로딩 씬에서: 모든 플레이어가 로딩 씬에 도착하면 맵으로 전환(마스터만)
        if (!_sceneTransition.IsLoadingScene(sceneName))
            return;

        _sceneTransition.TryStartLoadingGateIfReady();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 도착을 플레이어 프로퍼티로 남겨서 “모두 도착” 게이트에 사용
        if (PhotonNetwork.InRoom)
            _sceneTransition?.MarkLocalPlayerScene(scene.name);

        _sceneTransition?.SyncGameManagerState(scene.name);

        if (!PhotonNetwork.InRoom)
        {
            // 디버그 옵션이면 룸이 없어도 로비 씬에서 자동 매칭을 시작
            TryStartDebugQuickPlay(scene.name);
            return;
        }

        _sceneTransition?.HandleSceneLoadedInRoom(scene.name);

        if (!PhotonNetwork.IsMasterClient || _sceneTransition == null)
            return;

        if (_sceneTransition.IsLobbyScene(scene.name))
            _lobbyCountdown?.EvaluateLobbyCountdown();
    }

    internal void SetLocalPlayerProperty(string key, object value)
    {
        // CustomProperties는 부분 업데이트(키 단위)로만 갱신
        if (PhotonNetwork.LocalPlayer == null)
            return;

        PhotonHashtable props = new PhotonHashtable { { key, value } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    internal static bool GetBool(Player player, string key, bool defaultValue)
    {
        if (player == null || player.CustomProperties == null)
            return defaultValue;

        if (!player.CustomProperties.TryGetValue(key, out object raw))
            return defaultValue;

        return raw is bool b ? b : defaultValue;
    }

    internal static string GetString(Player player, string key, string defaultValue)
    {
        if (player == null || player.CustomProperties == null)
            return defaultValue;

        if (!player.CustomProperties.TryGetValue(key, out object raw))
            return defaultValue;

        return raw as string ?? defaultValue;
    }
}

internal static class GameObjectComponentExtensions
{
    internal static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
