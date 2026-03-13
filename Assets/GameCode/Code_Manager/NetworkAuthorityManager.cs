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
    public static NetworkAuthorityManager Instance { get; private set; }

    private const string PLAYER_PROP_READY = "ready";
    private const string PLAYER_PROP_SCENE = "scene";
    private const string ROOM_PROP_SELECTED_MAP = "selectedMap";

    [Header("Connection")]
    [SerializeField] private bool _autoConnect = true;
    [SerializeField] private bool _autoMatchmake = true;
    [SerializeField] private string _gameVersion = "1.0f";

    [Header("Room Options")]
    [SerializeField] private byte _maxPlayers = 8;
    [SerializeField] private bool _isRoomOpen = true;
    [SerializeField] private bool _isRoomVisible = true;

    [Header("Scenes")]
    [SerializeField] private string _titleSceneName = "Scene_Title&Login";
    [SerializeField] private string _lobbySceneName = "Scene_Lobby";
    [SerializeField] private string _loadingSceneName = "Scene_Loading";
    [SerializeField] private string _mapSceneName = "Scene_Map01";
    [SerializeField] private List<string> _mapSceneNames = new List<string>();

    [Header("Networking")]
    [SerializeField] private int _sendRate = 60;
    [SerializeField] private int _serializationRate = 60;
    [SerializeField] private float _loadingGateDelaySeconds = 0.25f;

    private Coroutine _loadingGateCoroutine;

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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // 자동 연결 옵션이면 시작 시점에만 연결 시도(중복 호출 방지)
        if (_autoConnect)
            ConnectIfNeeded();
    }

    public void ConnectIfNeeded()
    {
        // 이미 연결되어 있으면 아무것도 하지 않음(단일 책임 유지)
        if (PhotonNetwork.IsConnected)
            return;

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
        if (!_autoMatchmake)
            return;

        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinRandomRoom();
            return;
        }

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        // 로비 입장 완료 시 랜덤 룸 조인 시도
        if (!_autoMatchmake)
            return;

        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // 랜덤 조인 실패 시 룸 생성으로 폴백(빠른 매칭)
        if (!_autoMatchmake)
            return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = _maxPlayers,
            IsOpen = _isRoomOpen,
            IsVisible = _isRoomVisible
        };

        PhotonNetwork.CreateRoom(null, options);
    }

    public override void OnJoinedRoom()
    {
        // 룸 입장 직후: Ready/Scene 프로퍼티를 초기화(동기화 게이트 기반)
        SetLocalPlayerProperty(PLAYER_PROP_READY, true);
        SetLocalPlayerProperty(PLAYER_PROP_SCENE, SceneManager.GetActiveScene().name);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        // 로딩 씬에서: 모든 플레이어가 로딩 씬에 도착하면 맵으로 전환(마스터만)
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (SceneManager.GetActiveScene().name != _loadingSceneName)
            return;

        if (_loadingGateCoroutine != null)
            return;

        if (AreAllPlayersInScene(_loadingSceneName))
            _loadingGateCoroutine = StartCoroutine(LoadingGateToMap());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 도착을 플레이어 프로퍼티로 남겨서 “모두 도착” 게이트에 사용
        if (PhotonNetwork.InRoom)
            SetLocalPlayerProperty(PLAYER_PROP_SCENE, scene.name);

        SyncGameManagerState(scene.name);

        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (scene.name == _loadingSceneName)
        {
            if (_loadingGateCoroutine == null && AreAllPlayersInScene(_loadingSceneName))
                _loadingGateCoroutine = StartCoroutine(LoadingGateToMap());
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

    private void SelectAndStoreRandomMap()
    {
        // 라운드 시작 직전에 맵을 1개 뽑아 룸 프로퍼티로 고정(전원 동일 맵 보장)
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        List<string> candidates = GetCandidateMapSceneNames();
        string selected = candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : _mapSceneName;

        PhotonHashtable props = new PhotonHashtable
        {
            { ROOM_PROP_SELECTED_MAP, selected }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    private string GetSelectedMapSceneName()
    {
        // 룸 프로퍼티 기반으로 선택된 맵을 읽어옴(없으면 기본 맵)
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return _mapSceneName;

        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ROOM_PROP_SELECTED_MAP, out object raw) &&
            raw is string sceneName &&
            !string.IsNullOrWhiteSpace(sceneName))
        {
            return sceneName;
        }

        return _mapSceneName;
    }

    private List<string> GetCandidateMapSceneNames()
    {
        // 인스펙터 지정 맵이 있으면 우선 사용, 없으면 BuildSettings에서 Scene_Map* 자동 수집
        List<string> candidates = new List<string>();

        if (_mapSceneNames != null)
        {
            for (int i = 0; i < _mapSceneNames.Count; i++)
            {
                string name = _mapSceneNames[i];
                if (!string.IsNullOrWhiteSpace(name))
                    candidates.Add(name);
            }
        }

        if (candidates.Count > 0)
            return candidates;

        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (name.StartsWith("Scene_Map"))
                candidates.Add(name);
        }

        if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(_mapSceneName))
            candidates.Add(_mapSceneName);

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
        // 씬 이름을 GameState로 매핑(UI/채팅 시스템 메시지 등에 사용)
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        if (sceneName == _loadingSceneName)
            gm.SetGameState(GameState.Loading);
        else if (sceneName == _mapSceneName)
            gm.SetGameState(GameState.Playing);
        else if (sceneName == _titleSceneName || sceneName == _lobbySceneName)
            gm.SetGameState(GameState.Title);
    }
}
