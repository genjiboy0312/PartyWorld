using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Hex-A-Gone 아레나 전체 관리
/// - 모든 HexTile 추적
/// - 활성 타일 수 관리
/// - 플레이어 상태 추적
/// - 네트워크 동기화
/// </summary>
public class HexArenaManager : MonoBehaviourPunCallbacks
{
    public static HexArenaManager Instance { get; private set; }

    [Header("Tile Settings")]
    [SerializeField] private List<HexTile> _hexTiles = new List<HexTile>();
    [SerializeField] private Transform _tileContainer;

    [Header("Arena Settings")]
    [SerializeField] private int _activeTileCount = 0;
    [SerializeField] private int _totalTileCount = 0;
    [SerializeField] private int _sunkTileCount = 0;

    [Header("Player Tracking")]
    [SerializeField] private List<GameObject> _activePlayers = new List<GameObject>();
    [SerializeField] private int _alivePlayerCount = 0;

    [Header("Game Flow")]
    [SerializeField] private bool _isGameActive = false;

    // 타일 상태 동기화용 배열 (tileIndex -> durability)
    private int[] _tileDurabilities;
    private bool[] _tileSunkStates;

    // 이벤트
    public System.Action<int> TileSunk; // (남은 타일 수)
    public System.Action<int> PlayerEliminated; // (남은 플레이어 수)
    public System.Action OnLastTileSunk;
    public System.Action OnSinglePlayerLeft;

    // 프로퍼티
    public int ActiveTileCount => _activeTileCount;
    public int TotalTileCount => _totalTileCount;
    public int SunkTileCount => _sunkTileCount;
    public int AlivePlayerCount => _alivePlayerCount;
    public bool IsGameActive => _isGameActive;
    public List<HexTile> HexTiles => _hexTiles;

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
        // 씬의 모든 HexTile 자동 수집
        if (_tileContainer != null)
        {
            _hexTiles.Clear();
            _hexTiles.AddRange(_tileContainer.GetComponentsInChildren<HexTile>());

            // 각 타일에 tileIndex 설정
            for (int i = 0; i < _hexTiles.Count; i++)
            {
                _hexTiles[i].SetTileIndex(i);
            }
        }

        _totalTileCount = _hexTiles.Count;
        _activeTileCount = _totalTileCount;
        _sunkTileCount = 0;

        // 타일 상태 배열 초기화
        _tileDurabilities = new int[_totalTileCount];
        _tileSunkStates = new bool[_totalTileCount];
        for (int i = 0; i < _totalTileCount; i++)
        {
            _tileDurabilities[i] = _hexTiles[i].MaxDurability;
        }

        Debug.Log($"[HexArenaManager] Initialized with {_totalTileCount} tiles");
    }

    private void Update()
    {
        if (!_isGameActive)
            return;

        // 로컬 플레이어만 체크 (마스터가 전체 체크)
        if (!PhotonNetwork.IsMasterClient)
            return;

        CheckWinCondition();
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        _isGameActive = true;
        _sunkTileCount = 0;
        _activeTileCount = _totalTileCount;

        // RPC로 모든 클라이언트에 게임 시작 동기화
        if (PhotonNetwork.InRoom)
        {
            photonView.RPC("RPC_SyncGameStart", RpcTarget.All);
        }

        Debug.Log("[HexArenaManager] Game started");
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    public void EndGame()
    {
        _isGameActive = false;
        Debug.Log("[HexArenaManager] Game ended");
    }

    /// <summary>
    /// 모든 타일 리셋
    /// </summary>
    public void ResetAllTiles()
    {
        foreach (HexTile tile in _hexTiles)
        {
            tile.ResetTile();
        }

        // 딜레이 후 이동 재개
        StartCoroutine(ConfirmResetTiles());

        // RPC로 동기화
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SyncResetAllTiles", RpcTarget.All);
        }
    }

    private System.Collections.IEnumerator ConfirmResetTiles()
    {
        yield return new WaitForSeconds(0.1f);

        foreach (HexTile tile in _hexTiles)
        {
            tile.ConfirmReset();
        }

        _sunkTileCount = 0;
        _activeTileCount = _totalTileCount;

        // 상태 배열 리셋
        for (int i = 0; i < _totalTileCount; i++)
        {
            _tileDurabilities[i] = _hexTiles[i].MaxDurability;
            _tileSunkStates[i] = false;
        }
    }

    /// <summary>
    /// 플레이어가 탈락했을 때 호출
    /// </summary>
    public void HandlePlayerEliminated(GameObject player)
    {
        _activePlayers.Remove(player);
        _alivePlayerCount = _activePlayers.Count;

        Debug.Log($"[HexArenaManager] Player eliminated. Remaining: {_alivePlayerCount}");

        PlayerEliminated?.Invoke(_alivePlayerCount);

        // 마스터만 승자 판정
        if (PhotonNetwork.IsMasterClient)
        {
            CheckWinCondition();
        }
    }

    /// <summary>
    /// 타일이 가라앉았을 때 마스터에서 호출
    /// </summary>
    public void HandleTileSunk(int tileIndex)
    {
        if (!PhotonNetwork.InRoom)
        {
            // 오프라인 모드 - 직접 처리
            ProcessTileSunk(tileIndex);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
            return;

        // RPC로 동기화
        photonView.RPC("RPC_TileSunk", RpcTarget.All, tileIndex);
    }

    private void ProcessTileSunk(int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= _totalTileCount)
            return;

        _tileSunkStates[tileIndex] = true;
        _sunkTileCount++;
        _activeTileCount = _totalTileCount - _sunkTileCount;

        Debug.Log($"[HexArenaManager] Tile {tileIndex} sunk. Remaining tiles: {_activeTileCount}");

        TileSunk?.Invoke(_activeTileCount);

        // 마지막 타일이 가라앉았을 때
        if (_activeTileCount <= 1)
        {
            OnLastTileSunk?.Invoke();
        }
    }

    /// <summary>
    /// 타일이 데미지를 입었을 때 마스터에서 호출 (네트워크 동기화)
    /// </summary>
    public void OnTileDamaged(int tileIndex, int remainingDurability)
    {
        if (!PhotonNetwork.InRoom)
        {
            // 오프라인 모드 - 직접 처리
            ProcessTileDamaged(tileIndex, remainingDurability);
            return;
        }

        // 마스터에서만 처리
        if (!PhotonNetwork.IsMasterClient)
            return;

        // RPC로 동기화
        photonView.RPC("RPC_TileDamaged", RpcTarget.All, tileIndex, remainingDurability);
    }

    private void ProcessTileDamaged(int tileIndex, int remainingDurability)
    {
        if (tileIndex < 0 || tileIndex >= _totalTileCount)
            return;

        _tileDurabilities[tileIndex] = remainingDurability;

        // 로컬 타일에 적용
        if (tileIndex < _hexTiles.Count)
        {
            _hexTiles[tileIndex].ApplyNetworkState(remainingDurability);
        }
    }

    /// <summary>
    /// 플레이어 추가
    /// </summary>
    public void RegisterPlayer(GameObject player)
    {
        if (!_activePlayers.Contains(player))
        {
            _activePlayers.Add(player);
            _alivePlayerCount = _activePlayers.Count;
        }
    }

    /// <summary>
    /// 플레이어 제거
    /// </summary>
    public void UnregisterPlayer(GameObject player)
    {
        if (_activePlayers.Contains(player))
        {
            _activePlayers.Remove(player);
            _alivePlayerCount = _activePlayers.Count;
        }
    }

    /// <summary>
    /// 승리 조건 체크 (마스터만)
    /// </summary>
    private void CheckWinCondition()
    {
        if (!_isGameActive)
            return;

        // 1. 활성 플레이어 1명 남음
        if (_alivePlayerCount <= 1)
        {
            OnSinglePlayerLeft?.Invoke();
            return;
        }

        // 2. 활성 타일 1개 이하
        if (_activeTileCount <= 1)
        {
            OnLastTileSunk?.Invoke();
        }
    }

    /// <summary>
    /// 마지막 활성 타일 가져오기
    /// </summary>
    public HexTile GetLastActiveTile()
    {
        foreach (HexTile tile in _hexTiles)
        {
            if (!tile.IsSunk && !tile.IsSinking)
            {
                return tile;
            }
        }
        return null;
    }

    /// <summary>
    /// 아레나 통계 반환
    /// </summary>
    public string GetArenaStats()
    {
        return $"[HexArena] Tiles: {_activeTileCount}/{_totalTileCount} | Players: {_alivePlayerCount}";
    }

    #region Photon RPCs

    [PunRPC]
    private void RPC_TileDamaged(int tileIndex, int remainingDurability, PhotonMessageInfo info)
    {
        ProcessTileDamaged(tileIndex, remainingDurability);
    }

    [PunRPC]
    private void RPC_TileSunk(int tileIndex, PhotonMessageInfo info)
    {
        ProcessTileSunk(tileIndex);
    }

    [PunRPC]
    private void RPC_SyncGameStart(PhotonMessageInfo info)
    {
        _isGameActive = true;
        Debug.Log($"[HexArenaManager] Game started (synced from {info.Sender.NickName})");
    }

    [PunRPC]
    private void RPC_SyncResetAllTiles(PhotonMessageInfo info)
    {
        ResetAllTiles();
    }

    /// <summary>
    /// 전체 타일 상태 동기화 (새로운 클라이언트용)
    /// </summary>
    [PunRPC]
    private void RPC_SyncAllTileStates(int[] durabilities, bool[] sunkStates, PhotonMessageInfo info)
    {
        if (durabilities.Length != _totalTileCount || sunkStates.Length != _totalTileCount)
        {
            Debug.LogError("[HexArenaManager] Invalid tile state sync data");
            return;
        }

        for (int i = 0; i < _totalTileCount; i++)
        {
            _tileDurabilities[i] = durabilities[i];
            _tileSunkStates[i] = sunkStates[i];

            if (i < _hexTiles.Count)
            {
                _hexTiles[i].ApplyNetworkState(durabilities[i]);
                if (sunkStates[i])
                {
                    _hexTiles[i].ForceSink();
                }
            }
        }

        Debug.Log($"[HexArenaManager] Synced tile states from {info.Sender.NickName}");
    }

    /// <summary>
    /// 마스터에게 전체 상태 요청 (새로운 클라이언트가 입장 시 호출)
    /// </summary>
    public void RequestFullSync()
    {
        if (!PhotonNetwork.InRoom)
            return;

        if (PhotonNetwork.IsMasterClient)
        {
            // 마스터면 직접 전송
            photonView.RPC("RPC_SyncAllTileStates", RpcTarget.Others, _tileDurabilities, _tileSunkStates);
        }
        else
        {
            // 마스터에게 요청
            photonView.RPC("RPC_RequestFullSync", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    private void RPC_RequestFullSync(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC("RPC_SyncAllTileStates", info.Sender, _tileDurabilities, _tileSunkStates);
    }

    #endregion

    #region Photon Callbacks

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        Debug.Log($"[HexArenaManager] Player entered: {newPlayer.NickName}");

        // 마스터가 새로운 클라이언트에게 상태 동기화
        if (PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(RequestFullSync), 0.5f);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log($"[HexArenaManager] Player left: {otherPlayer.NickName}");
        
        // 방에 남은 플레이어 수 체크
        if (PhotonNetwork.PlayerList.Length <= 1)
        {
            OnSinglePlayerLeft?.Invoke();
        }
    }

    #endregion

    #region Editor Helpers

    /// <summary>
    /// 에디터에서 호출하여 타일 자동 수집
    /// </summary>
    [ContextMenu("Collect All HexTiles")]
    public void CollectAllTiles()
    {
#if UNITY_EDITOR
        if (_tileContainer != null)
        {
            _hexTiles.Clear();
            _hexTiles.AddRange(_tileContainer.GetComponentsInChildren<HexTile>());
            _totalTileCount = _hexTiles.Count;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[HexArenaManager] Collected {_totalTileCount} tiles");
        }
#endif
    }

    #endregion
}
