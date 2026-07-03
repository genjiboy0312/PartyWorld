using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// 육각형 타일들의 물리적 상태와 플레이어 생존을 관리하는 아레나 매니저
/// </summary>
[ExecuteAlways]
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

    // 타일 상태 동기화용 배열
    private int[] _tileDurabilities;
    private bool[] _tileSunkStates;

    // 이벤트
    public System.Action<int> TileSunk; 
    public System.Action<int> PlayerEliminated; 
    public System.Action OnLastTileSunk;
    public System.Action OnSinglePlayerLeft;

    // 프로퍼티
    public int ActiveTileCount => _activeTileCount;
    public int TotalTileCount => _totalTileCount;
    public int SunkTileCount => _sunkTileCount;
    public int AlivePlayerCount => _alivePlayerCount;
    public bool IsGameActive => _isGameActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!Application.isPlaying)
            InitializeTiles();
        }

    private void Start()
    {
        InitializeTiles();
    }

    private void InitializeTiles()
    {
        if (_tileContainer != null)
        {
            _hexTiles.Clear();
            _hexTiles.AddRange(_tileContainer.GetComponentsInChildren<HexTile>());

            // 층별로 내구도 차등 적용
            // 맨 위층 = Green (내구도 4), 아래로 갈수록 감소
            var floorGroups = _hexTiles
                .GroupBy(t => Mathf.Round(t.transform.position.y * 10f) / 10f)
                .OrderByDescending(g => g.Key)
                .ToList();

            for (int i = 0; i < floorGroups.Count; i++)
            {
                int floorDurability = Mathf.Max(0, 4 - i);
                foreach (var tile in floorGroups[i])
                {
                    tile.SetMaxDurability(floorDurability);
                }
            }

            for (int i = 0; i < _hexTiles.Count; i++)
            {
                _hexTiles[i].SetTileIndex(i);
            }
        }

        _totalTileCount = _hexTiles.Count;
        _activeTileCount = _totalTileCount;
        _sunkTileCount = 0;

        _tileDurabilities = new int[_totalTileCount];
        _tileSunkStates = new bool[_totalTileCount];
        for (int i = 0; i < _totalTileCount; i++)
        {
            _tileDurabilities[i] = _hexTiles[i].MaxDurability;
        }

        Debug.Log($"[HexArenaManager] Initialized with {_totalTileCount} tiles");
    }

    public void StartGame()
    {
        _isGameActive = true;
        _sunkTileCount = 0;
        _activeTileCount = _totalTileCount;

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_SyncGameStart), RpcTarget.All);
        }
    }

    public void EndGame()
    {
        _isGameActive = false;
        Debug.Log("[HexArenaManager] Game ended");
    }

    public void ResetAllTiles()
    {
        foreach (HexTile tile in _hexTiles)
            tile.ResetTile();

        StartCoroutine(ConfirmResetTiles());

        // RPC recursive fix: send to Others only (not self)
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_SyncResetAllTiles), RpcTarget.Others);
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

    public void HandleTileSunk(int tileIndex)
    {
        if (!PhotonNetwork.InRoom)
        {
            ProcessTileSunk(tileIndex);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC(nameof(RPC_TileSunk), RpcTarget.All, tileIndex);
    }

    private void ProcessTileSunk(int tileIndex)
    {
        if (tileIndex < 0 || tileIndex >= _totalTileCount || _tileSunkStates[tileIndex])
            return;

        _tileSunkStates[tileIndex] = true;
        _sunkTileCount++;
        _activeTileCount = _totalTileCount - _sunkTileCount;

        TileSunk?.Invoke(_activeTileCount);

        if (_activeTileCount <= 1)
        {
            OnLastTileSunk?.Invoke();
        }
    }

    public void OnTileDamaged(int tileIndex, int remainingDurability)
    {
        if (!PhotonNetwork.InRoom)
        {
            ProcessTileDamaged(tileIndex, remainingDurability);
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
            return;

        photonView.RPC(nameof(RPC_TileDamaged), RpcTarget.All, tileIndex, remainingDurability);
    }

    private void ProcessTileDamaged(int tileIndex, int remainingDurability)
    {
        if (tileIndex < 0 || tileIndex >= _totalTileCount)
            return;

        _tileDurabilities[tileIndex] = remainingDurability;
        if (tileIndex < _hexTiles.Count)
        {
            _hexTiles[tileIndex].ApplyNetworkState(remainingDurability);
        }
    }

    public void RegisterPlayer(GameObject player)
    {
        if (!_activePlayers.Contains(player))
        {
            _activePlayers.Add(player);
            _alivePlayerCount = _activePlayers.Count;
        }
    }

    public void UnregisterPlayer(GameObject player)
    {
        if (_activePlayers.Contains(player))
        {
            _activePlayers.Remove(player);
            _alivePlayerCount = _activePlayers.Count;
        }
        
        PlayerEliminated?.Invoke(_alivePlayerCount);
        
        if (PhotonNetwork.IsMasterClient)
        {
            CheckWinCondition();
        }
    }

    /// <summary>
    /// 플레이어 탈락 처리 (BubbleZone 등에서 호출)
    /// </summary>
    public void HandlePlayerEliminated(GameObject player)
    {
        UnregisterPlayer(player);
    }

    private void CheckWinCondition()
    {
        if (!_isGameActive) return;

        if (_alivePlayerCount <= 1)
        {
            OnSinglePlayerLeft?.Invoke();
        }
    }

    #region RPCs
    [PunRPC]
    private void RPC_SyncGameStart() { _isGameActive = true; }

    [PunRPC]
    private void RPC_SyncResetAllTiles()
    {
        // Direct tile reset (NOT calling ResetAllTiles() to avoid RPC recursion)
        foreach (HexTile tile in _hexTiles)
            tile.ResetTile();

        StartCoroutine(ConfirmResetTiles());
    }

    [PunRPC]
    private void RPC_TileSunk(int index) { ProcessTileSunk(index); }

    [PunRPC]
    private void RPC_TileDamaged(int index, int dur) { ProcessTileDamaged(index, dur); }
    #endregion
}
