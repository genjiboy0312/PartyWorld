using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class ChatManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private Text[] _chatText;
    [SerializeField] private InputField _chatInput;
    [SerializeField] private Button _sendBtn;
    [SerializeField] private GameObject _textHide;

    [Header("Connection")]
    [SerializeField] private bool _connectOnStart = true;
    [SerializeField] private bool _autoStartQuickPlayOnSendIfNotInRoom = false;
    [SerializeField] private bool _autoJoinChatRoomInWaitingRoom = true;
    [SerializeField] private string _waitingRoomSceneName = "Scene_WaitingRoom";
    [SerializeField] private string _waitingRoomChatRoomName = "Room_WaitingRoomChat";
    [SerializeField] private byte _waitingRoomChatMaxPlayers = 20;

    [Header("ETC")]
    [SerializeField] private Text _statusText;

    private PhotonView _pv;
    private GameManager _gameManager;
    private FirebaseAuthManager _authManager;
    private bool _requestedChatRoomJoin;

    private void Awake()
    {
        _pv = GetComponent<PhotonView>();

        if (_pv == null)
        {
            Debug.LogError("[ChatManager] PhotonView 컴포넌트가 없습니다!");
        }

        // GameManager 캐싱 및 구독
        _gameManager = GameManager.Instance;
        if (_gameManager != null)
        {
            _gameManager.OnGameStateChangeEvent += OnGameStateChange;
        }
        else
        {
            Debug.LogWarning("[ChatManager] GameManager.Instance가 null입니다!");
        }

        // FirebaseAuthManager 캐싱
        _authManager = FirebaseAuthManager.Instance;
        if (_authManager == null)
        {
            Debug.LogError("[ChatManager] FirebaseAuthManager.Instance가 null입니다!");
        }
    }

    private void Start()
    {
        // Photon NickName 설정
        if (DataManager.Instance != null &&
            !string.IsNullOrWhiteSpace(DataManager.Instance.CurrentUserData.nickname) &&
            DataManager.Instance.CurrentUserData.nickname != "NewPlayer")
        {
            PhotonNetwork.NickName = DataManager.Instance.CurrentUserData.nickname;
        }
        else if (_authManager != null)
        {
            if (!string.IsNullOrWhiteSpace(_authManager._userEmail) && _authManager._userEmail != "None")
                PhotonNetwork.NickName = _authManager._userEmail;
        }

        if (_connectOnStart)
        {
            Connect();
        }

        TryJoinWaitingRoomChatIfNeeded();

        // Send 버튼 이벤트 연결
        if (_sendBtn != null)
        {
            _sendBtn.onClick.AddListener(Send);
        }
        else
        {
            Debug.LogWarning("[ChatManager] Send 버튼이 할당되지 않았습니다.");
        }

        // InputField Enter 키 처리
        if (_chatInput != null)
        {
            _chatInput.onEndEdit.AddListener(OnInputEndEdit);
        }
    }

    private void Update()
    {
        // 서버 상태 표시
        if (_statusText != null)
        {
            _statusText.text = PhotonNetwork.NetworkClientState.ToString();
        }

        RefreshSendInteractivity();

        // UI 외 영역 클릭 시 활성화
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            SetTextActive(true);
        }

        // ESC 키로 채팅 숨기기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetTextActive(false);
        }
    }

    public override void OnDisable()
    {
        UnsubscribeEvents();
        base.OnDisable();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        // UI 이벤트 정리
        if (_sendBtn != null)
        {
            _sendBtn.onClick.RemoveListener(Send);
        }

        if (_chatInput != null)
        {
            _chatInput.onEndEdit.RemoveListener(OnInputEndEdit);
        }
    }

    private void UnsubscribeEvents()
    {
        if (_gameManager != null)
        {
            _gameManager.OnGameStateChangeEvent -= OnGameStateChange;
        }
    }

    #region Photon 서버 연결

    public void Connect()
    {
        if (NetworkAuthorityManager.Instance != null)
        {
            NetworkAuthorityManager.Instance.ConnectIfNeeded();
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[ChatManager] Photon 마스터 서버 연결 성공");
        TryJoinWaitingRoomChatIfNeeded();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[ChatManager] 방 입장 성공: {PhotonNetwork.CurrentRoom.Name}");

        string msg = $"<color=blue>*** {PhotonNetwork.NickName}님이 입장했습니다. ***</color>";
        AddChatMessage(msg);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[ChatManager] Photon 서버 연결 해제: {cause}");
        _requestedChatRoomJoin = false;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[ChatManager] OnJoinRoomFailed (code={returnCode}, msg={message}, state={PhotonNetwork.NetworkClientState})");
        _requestedChatRoomJoin = false;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[ChatManager] OnCreateRoomFailed (code={returnCode}, msg={message}, state={PhotonNetwork.NetworkClientState})");
        _requestedChatRoomJoin = false;
    }

    #endregion

    #region 플레이어 입장/퇴장

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        string msg = $"<color=green>[{newPlayer.NickName}]님이 입장하셨습니다.</color>";
        AddChatMessage(msg);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        string msg = $"<color=red>[{otherPlayer.NickName}]님이 퇴장하셨습니다.</color>";
        AddChatMessage(msg);
    }

    #endregion

    #region 채팅

    // Enter 키로 전송
    private void OnInputEndEdit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Send();
        }
    }

    // Send 버튼 또는 Enter 키 호출
    public void Send()
    {
        if (_chatInput == null || string.IsNullOrWhiteSpace(_chatInput.text))
        {
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            AddChatMessage($"<color=yellow>*** 아직 방에 입장하지 않았습니다. (state={PhotonNetwork.NetworkClientState}) ***</color>");

            if (_autoStartQuickPlayOnSendIfNotInRoom)
            {
                NetworkAuthorityManager.Instance?.StartQuickPlay();
            }
            else
            {
                TryJoinWaitingRoomChatIfNeeded();
            }
            return;
        }

        if (_pv == null)
        {
            Debug.LogError("[ChatManager] PhotonView가 null입니다!");
            return;
        }

        string nickname = !string.IsNullOrWhiteSpace(PhotonNetwork.NickName) ? PhotonNetwork.NickName : "Unknown";
        string formattedMessage = $"<color=white>[{nickname}]</color> {_chatInput.text}";

        _pv.RPC(nameof(ChatRPC), RpcTarget.All, formattedMessage);

        // 입력 필드 초기화 및 포커스
        _chatInput.text = "";
        _chatInput.ActivateInputField();
    }

    [PunRPC]
    private void ChatRPC(string msg)
    {
        AddChatMessage(msg);
    }

    // 메시지 밀기 로직 (최적화)
    private void AddChatMessage(string msg)
    {
        if (_chatText == null || _chatText.Length == 0)
        {
            Debug.LogWarning("[ChatManager] _chatText 배열이 비어있습니다.");
            return;
        }

        // 메시지를 위로 밀기
        for (int i = 0; i < _chatText.Length - 1; i++)
        {
            if (_chatText[i] != null && _chatText[i + 1] != null)
            {
                _chatText[i].text = _chatText[i + 1].text;
            }
        }

        // 마지막 줄에 새 메시지 추가
        if (_chatText[_chatText.Length - 1] != null)
        {
            _chatText[_chatText.Length - 1].text = msg;
        }

        Debug.Log($"[ChatManager] 메시지 추가: {msg}");
    }

    #endregion

    #region GameManager 연동 (옵저버 패턴)

    // GameState 변경 시 호출됨
    public void OnGameStateChange(GameState newGameState)
    {
        if (_pv == null)
        {
            Debug.LogWarning("[ChatManager] PhotonView가 null이어서 시스템 메시지를 전송할 수 없습니다.");
            return;
        }

        switch (newGameState)
        {
            case GameState.Title:
                SendSystemMessage("*** 게임 대기 중 ***");
                break;

            case GameState.Loading:
                SendSystemMessage("*** 로딩 중... ***");
                break;

            case GameState.Playing:
                SendSystemMessage("*** 게임 시작! ***");
                break;

            case GameState.GameOver:
                SendSystemMessage("*** 게임 종료 ***");
                break;
        }

        Debug.Log($"[ChatManager] GameState 변경: {newGameState}");
    }

    private void SendSystemMessage(string message)
    {
        if (_pv != null && PhotonNetwork.InRoom)
        {
            _pv.RPC(nameof(ChatRPC), RpcTarget.All, $"<color=yellow>{message}</color>");
        }
    }

    #endregion

    private void RefreshSendInteractivity()
    {
        bool canSend = PhotonNetwork.InRoom;

        if (_sendBtn != null && _sendBtn.interactable != canSend)
            _sendBtn.interactable = canSend;

        if (_chatInput != null && _chatInput.interactable != canSend)
            _chatInput.interactable = canSend;
    }

    private void TryJoinWaitingRoomChatIfNeeded()
    {
        if (!_autoJoinChatRoomInWaitingRoom)
            return;

        if (_requestedChatRoomJoin)
            return;

        string activeScene = SceneManager.GetActiveScene().name;
        if (!string.Equals(activeScene, _waitingRoomSceneName))
            return;

        if (!PhotonNetwork.IsConnectedAndReady)
            return;

        if (PhotonNetwork.InRoom)
            return;

        if (string.IsNullOrWhiteSpace(_waitingRoomChatRoomName))
        {
            Debug.LogError("[ChatManager] WaitingRoom chat room name is empty.");
            return;
        }

        _requestedChatRoomJoin = true;

        RoomOptions options = new RoomOptions
        {
            IsOpen = true,
            IsVisible = true,
            MaxPlayers = _waitingRoomChatMaxPlayers
        };

        PhotonNetwork.JoinOrCreateRoom(_waitingRoomChatRoomName, options, TypedLobby.Default);
        Debug.Log($"[ChatManager] JoinOrCreateRoom({_waitingRoomChatRoomName}) requested (scene={activeScene}).");
    }

    #region UI 활성화/비활성화

    public void OnTextClick()
    {
        SetTextActive(false);
    }

    private void SetTextActive(bool isActive)
    {
        if (_textHide != null)
        {
            _textHide.SetActive(isActive);
        }
    }
    #endregion
}
