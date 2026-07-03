using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

[DisallowMultipleComponent]
public sealed class SceneTransitionManager : MonoBehaviour
{
    internal const string PLAYER_PROP_SCENE = "scene";
    private const string ROOM_PROP_SELECTED_MAP = "selectedMap";

    [Header("Scenes")]
    [SerializeField] private SceneReference _titleScene = new SceneReference();
    [SerializeField] private SceneReference _waitingRoomScene = new SceneReference();
    [SerializeField] private SceneReference _roomLobbyScene = new SceneReference();
    [SerializeField] private SceneReference _loadingScene = new SceneReference();
    [SerializeField] private List<SceneReference> _playMapScenes = new List<SceneReference>();
    [SerializeField] private string _mapScenePrefix = "Scene_Map";

    [Header("Networking")]
    [SerializeField] private float _loadingGateDelaySeconds = 0.25f;

    private string _titleSceneName;
    private string _waitingRoomSceneName;
    private string _roomLobbySceneName;
    private string _loadingSceneName;

    private Coroutine _loadingGateCoroutine;
    private NetworkAuthorityManager _authority;
    private CharacterSpawnManager _characterSpawn;

    internal string WaitingRoomSceneName => _waitingRoomSceneName;
    internal string RoomLobbySceneName => _roomLobbySceneName;
    internal string LoadingSceneName => _loadingSceneName;

    private void Awake()
    {
        CacheSceneNames();
    }

    internal void Configure(
        NetworkAuthorityManager authority,
        CharacterSpawnManager characterSpawn,
        SceneReference titleScene,
        SceneReference waitingRoomScene,
        SceneReference roomLobbyScene,
        SceneReference loadingScene,
        List<SceneReference> playMapScenes,
        string mapScenePrefix,
        float loadingGateDelaySeconds)
    {
        _authority = authority;
        _characterSpawn = characterSpawn;
        _titleScene = titleScene ?? _titleScene;
        _waitingRoomScene = waitingRoomScene ?? _waitingRoomScene;
        _roomLobbyScene = roomLobbyScene ?? _roomLobbyScene;
        _loadingScene = loadingScene ?? _loadingScene;
        _playMapScenes = playMapScenes != null
            ? new List<SceneReference>(playMapScenes)
            : new List<SceneReference>();
        _mapScenePrefix = mapScenePrefix;
        _loadingGateDelaySeconds = loadingGateDelaySeconds;
        CacheSceneNames();
    }

    internal void MarkLocalPlayerScene(string sceneName)
    {
        _authority?.SetLocalPlayerProperty(PLAYER_PROP_SCENE, sceneName);
    }

    internal void LoadWaitingRoomIfNeeded(bool shouldLoadWaitingRoom)
    {
        if (!shouldLoadWaitingRoom)
            return;

        if (SceneManager.GetActiveScene().name != _waitingRoomSceneName)
            PhotonNetwork.LoadLevel(_waitingRoomSceneName);
    }

    internal void RequestStartMatch()
    {
        // 마스터만 로딩/맵 전환을 트리거(권위 단일화). Ready 체크는 NetworkAuthorityManager가 담당.
        if (!PhotonNetwork.InRoom)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        SelectAndStoreRandomMap();
        LoadLoadingScene();
    }

    internal void HandleSceneLoadedInRoom(string sceneName)
    {
        if (IsMapScene(sceneName))
        {
            _characterSpawn?.TrySpawnLocalPlayerForMap();
            InitializeRoundManagerForMap(sceneName);
        }

        if (!PhotonNetwork.IsMasterClient)
            return;

        if (IsLoadingScene(sceneName))
            TryStartLoadingGateIfReady();
    }

    internal void TryStartLoadingGateIfReady()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (!IsLoadingScene(SceneManager.GetActiveScene().name))
            return;

        if (_loadingGateCoroutine == null && AreAllPlayersInScene(_loadingSceneName))
            _loadingGateCoroutine = StartCoroutine(LoadingGateToMap());
    }

    internal void LoadLoadingScene()
    {
        PhotonNetwork.LoadLevel(_loadingSceneName);
    }

    internal void ReturnToLobby()
    {
        if (!PhotonNetwork.InRoom)
            return;

        // RoundManager 세션 정리
        if (RoundManager.Instance != null)
            RoundManager.Instance.CleanupSession();

        PhotonNetwork.LoadLevel(_roomLobbySceneName);
    }

    internal bool IsLobbyScene(string sceneName)
    {
        return sceneName == _roomLobbySceneName || sceneName == _waitingRoomSceneName;
    }

    internal bool IsLoadingScene(string sceneName)
    {
        return sceneName == _loadingSceneName;
    }

    internal bool IsMapScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
            !string.IsNullOrWhiteSpace(_mapScenePrefix) &&
            sceneName.StartsWith(_mapScenePrefix, StringComparison.OrdinalIgnoreCase);
    }

    internal void SyncGameManagerState(string sceneName)
    {
        // 씬 상태 매핑은 GameManager가 단일 책임으로 관리
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;

        gm.SyncStateByScene(sceneName);
    }

    internal void SelectAndStoreRandomMap()
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

        string selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        PhotonHashtable props = new PhotonHashtable
        {
            { ROOM_PROP_SELECTED_MAP, selected }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    internal string GetSelectedMapSceneName()
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

    internal List<string> GetCandidateMapSceneNames()
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

    internal bool AreAllPlayersInScene(string sceneName)
    {
        // 로딩 게이트: 모든 플레이어의 scene 프로퍼티가 목표 씬인지 확인
        if (!PhotonNetwork.InRoom)
            return false;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            string playerScene = NetworkAuthorityManager.GetString(p, PLAYER_PROP_SCENE, string.Empty);
            if (playerScene != sceneName)
                return false;
        }

        return true;
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

    private void InitializeRoundManagerForMap(string mapSceneName)
    {
        if (RoundManager.Instance == null)
        {
            Debug.LogWarning("[NetworkAuthorityManager] RoundManager not found. Creating one.");
            gameObject.AddComponent<RoundManager>();
        }

        RoundManager.Instance.InitializeRoundSession(mapSceneName);
    }

    private void CacheSceneNames()
    {
        _titleSceneName = _titleScene?.SceneName ?? string.Empty;
        _waitingRoomSceneName = _waitingRoomScene?.SceneName ?? string.Empty;
        _roomLobbySceneName = _roomLobbyScene?.SceneName ?? string.Empty;
        _loadingSceneName = _loadingScene?.SceneName ?? string.Empty;
    }
}
