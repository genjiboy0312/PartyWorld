using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// 방 목록 데이터 관리 및 Photon 로비 동기화를 전담하는 매니저 (싱글톤)
///
/// [담당职责]
/// - Photon 로비 연결 및 방 목록 수신
/// - RoomInfo → RoomData 변환 및 캐싱
/// - 가상 방(테스트용) 관리
/// - UI 이벤트 발행 (OnRoomListChanged)
///
/// [사용법]
/// RoomManager.Instance.JoinRoom("방이름");
/// RoomManager.Instance.CreateRoom("방이름");
/// RoomManager.Instance.OnRoomListChanged += OnRoomListChanged;
/// </summary>
public class RoomManager : MonoBehaviourPunCallbacks
{
    // ── 싱글톤 ──
    public static RoomManager Instance { get; private set; }

    // ── Const ──
    private static readonly string[] VirtualRoomNames = { "Test01", "Test02" };

    [Header("Scene Transition")]
    [SerializeField] private string _waitingRoomSceneName = "Scene_WaitingRoom";

    // ── Room creation fallback flag (NetworkAuthorityManager absent) ──
    private bool _localCreateRequested;

    // ── 방 목록 ──
    private List<RoomData> _roomList = new List<RoomData>();
    public IReadOnlyList<RoomData> RoomList => _roomList;

    // ── 로비 상태 ──
    public bool IsInLobby { get; private set; }
    public int RoomCount => _roomList.Count;

    // ── 이벤트 ──
    public event Action OnRoomListChanged;
    public event Action<string> OnStatusMessage;

    // =================================================================
    // Lifecycle
    // =================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject);  // Manager_Room is child of Manager; only needed in Lobby
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // =================================================================
    // Public API — 방 목록
    // =================================================================

    /// <summary>외부에서 강제로 방 목록 갱신 (로비 재입장)</summary>
    public void RequestRefresh()
    {
        if (!IsInLobby)
        {
            TryConnectAndJoinLobby();
            return;
        }

        EmitStatus("방 목록 새로 고치는 중...");
        _roomList.Clear();
        PhotonNetwork.LeaveLobby();
    }

    /// <summary>현재 캐싱된 방 목록을 복사본으로 반환</summary>
    public List<RoomData> GetRoomListCopy()
    {
        return new List<RoomData>(_roomList);
    }

    /// <summary>특정 방 데이터 조회 (없으면 null 반환)</summary>
    public RoomData GetRoomData(string roomName)
    {
        return _roomList.Find(r => r.roomName == roomName);
    }

    // =================================================================
    // Public API — 방 생성 / 입장
    // =================================================================

    /// <summary>일반 방 입장 요청</summary>
    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            EmitStatus("서버에 연결되어 있지 않습니다.", true);
            return;
        }
        EmitStatus($"\"{roomName}\" 방에 입장 중...");
        PhotonNetwork.JoinRoom(roomName);
    }

    /// <summary>가상 방 → 실제 방 생성 후 입장</summary>
    public void CreateVirtualRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            EmitStatus("서버에 연결되어 있지 않습니다.", true);
            return;
        }
        EmitStatus($"\"{roomName}\" 방 생성 중...");
        RoomOptions options = new RoomOptions { MaxPlayers = 8, IsOpen = true, IsVisible = true };

        if (NetworkAuthorityManager.Instance != null)
        {
            NetworkAuthorityManager.Instance.RequestCreateRoom(roomName, options);
        }
        else
        {
            _localCreateRequested = true;
            PhotonNetwork.CreateRoom(roomName, options);
        }
    }
    /// <summary>입력한 이름으로 방 생성 요청</summary>
    public void RequestCreateRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            EmitStatus("서버에 연결되어 있지 않습니다.", true);
            return;
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 8,
            IsOpen = true,
            IsVisible = true
        };

        EmitStatus($"\"{roomName}\" 방 생성 중...");
        if (NetworkAuthorityManager.Instance != null)
        {
            NetworkAuthorityManager.Instance.RequestCreateRoom(roomName, options);
        }
        else
        {
            _localCreateRequested = true;
            PhotonNetwork.CreateRoom(roomName, options);
        }
    }
    /// <summary>빠른 매칭 요청 (현재 방 목록에서 랜덤 입장)</summary>
    public void RequestQuickMatch()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            EmitStatus("서버에 연결되어 있지 않습니다.", true);
            return;
        }

        // 모든 방(가상+실제) 중 입장/생성 가능한 것만 필터링
        var available = _roomList.FindAll(r => r.isOpen && r.playerCount < r.maxPlayers);

        if (available.Count > 0)
        {
            RoomData pick = available[UnityEngine.Random.Range(0, available.Count)];
            EmitStatus($"[{pick.roomName}] 방에 무작위 입장 중...");
            if (pick.isVirtual)
                CreateVirtualRoom(pick.roomName);
            else
                PhotonNetwork.JoinRoom(pick.roomName);
        }
        else
        {
            EmitStatus("입장 가능한 방이 없습니다. 새 방을 생성합니다...");
            if (NetworkAuthorityManager.Instance != null)
            {
                NetworkAuthorityManager.Instance.StartQuickPlay();
            }
            else
            {
                _localCreateRequested = true;
                PhotonNetwork.JoinRandomOrCreateRoom();
            }
        }
    }
    // =================================================================
    // Public API — 로비 연결
    // =================================================================

    /// <summary>UI에서 방 목록을 열 때 호출</summary>
    public void OpenRoomList()
    {
        TryConnectAndJoinLobby();
    }

    /// <summary>UI에서 방 목록을 닫을 때 정리</summary>
    public void CloseRoomList()
    {
        ClientState state = PhotonNetwork.NetworkClientState;
        if (state != ClientState.Joining &&
            state != ClientState.ConnectingToGameServer &&
            state != ClientState.Leaving)
        {
            if (IsInLobby && PhotonNetwork.InLobby)
                PhotonNetwork.LeaveLobby();
        }
        IsInLobby = false;
        _roomList.Clear();
    }

    // =================================================================
    // Photon Callbacks — 로비 / 방 목록
    // =================================================================

    public override void OnConnectedToMaster()
    {
        if (isActiveAndEnabled && !PhotonNetwork.InLobby && !IsInLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        IsInLobby = true;
        EmitStatus("방 목록을 불러오는 중...");
    }

    public override void OnLeftLobby()
    {
        IsInLobby = false;
        ClientState state = PhotonNetwork.NetworkClientState;
        if (state == ClientState.Joining ||
            state == ClientState.ConnectingToGameServer ||
            state == ClientState.Leaving)
            return;
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        SyncRoomList(roomList);
        NotifyRoomListChanged();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        IsInLobby = false;
        EmitStatus("연결이 끊겼습니다.", true);
    }

    // =================================================================
    // Photon Callbacks — 방 입장 결과
    // =================================================================

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[RoomManager] CreateRoomFailed: {message}");
        EmitStatus($"방 만들기 실패: {message}", true);
        IsInLobby = false;
        TryConnectAndJoinLobby();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[RoomManager] JoinRoomFailed: {message}");
        EmitStatus($"입장 실패: {message}", true);
        IsInLobby = false;
        TryConnectAndJoinLobby();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[RoomManager] OnJoinedRoom (room={PhotonNetwork.CurrentRoom?.Name})");

        // RoomManager가 방 생성/입장을 요청했고 NetworkAuthorityManager가 없으면
        // 직접 WaitingRoom 씬으로 전환
        if (_localCreateRequested)
        {
            _localCreateRequested = false;
            if (NetworkAuthorityManager.Instance == null && !string.IsNullOrEmpty(_waitingRoomSceneName))
            {
                EmitStatus("WaitingRoom으로 이동 중...");
                PhotonNetwork.LoadLevel(_waitingRoomSceneName);
            }
        }
    }

    // =================================================================
    // 내부 — 방 목록 동기화
    // =================================================================

    private void SyncRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            int idx = _roomList.FindIndex(r => r.roomName == info.Name);
            if (info.RemovedFromList)
            {
                if (idx >= 0)
                    _roomList.RemoveAt(idx);
            }
            else
            {
                RoomData data = ToRoomData(info);
                if (idx >= 0)
                    _roomList[idx] = data;
                else
                    _roomList.Add(data);
            }
        }

        // Sort: 꽉 찬 방은 아래로, 그 외에는 플레이어 많은 순
        _roomList.Sort((a, b) =>
        {
            int aFull = a.playerCount >= a.maxPlayers ? 1 : 0;
            int bFull = b.playerCount >= b.maxPlayers ? 1 : 0;
            if (aFull != bFull) return aFull.CompareTo(bFull);
            return b.playerCount.CompareTo(a.playerCount);
        });

        // 방 번호 재할당
        for (int i = 0; i < _roomList.Count; i++)
            _roomList[i].roomNumber = i + 1;

        // 가상 방 보충: 실제 방이 2개 미만이면 테스트 방을 채움
        if (_roomList.Count < VirtualRoomNames.Length)
        {
            int virtualIndex = 0;
            for (int i = _roomList.Count; i < VirtualRoomNames.Length; i++)
            {
                string vName = VirtualRoomNames[virtualIndex++];
                // 이미 같은 이름의 실제 방이 있으면 스킵
                if (_roomList.Exists(r => r.roomName == vName))
                    continue;

                _roomList.Add(new RoomData(i + 1, vName, 0, 8, true, true, true));
            }
        }
    }

    // =================================================================
    // 내부 — 로비 연결
    // =================================================================

    private void TryConnectAndJoinLobby()
    {
        if (PhotonNetwork.InRoom)
        {
            EmitStatus("이미 방에 입장해 있습니다.");
            return;
        }

        if (PhotonNetwork.InLobby)
        {
            IsInLobby = true;
            NotifyRoomListChanged();
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            EmitStatus("서버에 연결 중...");
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        if (IsInLobby) return;

        EmitStatus("로비에 입장 중...");
        PhotonNetwork.JoinLobby();
    }

    // =================================================================
    // 내부 — 헬퍼
    // =================================================================

    private void EmitStatus(string msg, bool isError = false)
    {
        Debug.Log($"[RoomManager] {(isError ? "ERR" : "INF")}: {msg}");
        OnStatusMessage?.Invoke(msg);
    }

    private void NotifyRoomListChanged()
    {
        OnRoomListChanged?.Invoke();
    }

    private static RoomData ToRoomData(RoomInfo info)
    {
        return new RoomData(
            roomNumber: 0, // SyncRoomList에서 재할당
            roomName: info.Name,
            playerCount: info.PlayerCount,
            maxPlayers: info.MaxPlayers,
            isOpen: info.IsOpen,
            isVisible: info.IsVisible,
            isVirtual: false
        );
    }
}
