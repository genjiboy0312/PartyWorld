using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// Hex-A-Gone 아레나 전체 관리
/// - 모든 HexTile 추적
/// - 활성 타일 수 관리
/// - 플레이어 상태 추적
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
        }

        _totalTileCount = _hexTiles.Count;
        _activeTileCount = _totalTileCount;
        _sunkTileCount = 0;

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
    public void HandleTileSunk(HexTile tile)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        _sunkTileCount++;
        _activeTileCount = _totalTileCount - _sunkTileCount;

        Debug.Log($"[HexArenaManager] Tile sunk. Remaining tiles: {_activeTileCount}");

        TileSunk?.Invoke(_activeTileCount);

        // 마지막 타일이 가라앉았을 때
        if (_activeTileCount <= 1)
        {
            OnLastTileSunk?.Invoke();
        }
    }

    /// <summary>
    /// 타일이 데미지를 입었을 때 (네트워크 동기화용)
    /// </summary>
    public void OnTileDamaged(HexTile tile, int remainingDurability)
    {
        // 마스터에서만 처리
        if (!PhotonNetwork.IsMasterClient)
            return;

        // 네트워크로 동기화 (선택사항)
        if (PhotonNetwork.InRoom)
        {
            // RPC 또는 CustomProperties로 동기화
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
            // 마지막 타일 위에 있는 플레이어가 승리
            HexTile lastTile = GetLastActiveTile();
            if (lastTile != null)
            {
                // 타일 위에 있는 플레이어 확인
                // TODO: 실제 플레이어 위치 체크
            }
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
    /// 가장 많은 타일 위에 있는 플레이어 가져오기
    /// </summary>
    public GameObject GetPlayerOnMostTiles()
    {
        // TODO: 각 플레이어별 밟은 타일 수 추적 로직
        return null;
    }

    /// <summary>
    /// 아레나 통계 반환
    /// </summary>
    public string GetArenaStats()
    {
        return $"[HexArena] Tiles: {_activeTileCount}/{_totalTileCount} | Players: {_alivePlayerCount}";
    }

    #region Photon Callbacks

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
