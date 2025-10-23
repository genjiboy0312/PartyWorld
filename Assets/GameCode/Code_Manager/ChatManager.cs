using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.EventSystems;

public class ChatManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private Text[] _chatText;         // 채팅 메시지 표시 텍스트 배열
    [SerializeField] private InputField _chatInput;   // 채팅 입력 필드
    [SerializeField] private Button _sendBtn;         // 채팅 전송 버튼
    [SerializeField] private GameObject _textHide;    // 채팅 UI 비활성화용 오브젝트

    [Header("ETC")]
    [SerializeField] private Text StatusText;         // 서버 상태 표시

    private PhotonView _pv;

    private void Awake()
    {
        _pv = GetComponent<PhotonView>();

        // GameManager 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance._onGameStateChange += OnGameStateChange;
        }
    }

    private void Start()
    {
        // 로그인한 이메일로 Photon NickName 설정
        PhotonNetwork.NickName = FirebaseAuthManager.Instance._userEmail;

        // Photon 서버 연결
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.IsMessageQueueRunning = true;

        // Send 버튼에 이벤트 연결
        if (_sendBtn != null)
            _sendBtn.onClick.AddListener(Send);
    }

    private void Update()
    {
        // 서버 상태 표시
        if (StatusText != null)
            StatusText.text = PhotonNetwork.NetworkClientState.ToString();

        // UI 외 영역 클릭 시 활성화
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            SetTextActive(true);
        }
    }

    #region Photon 서버 연결

    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster()
    {
        // Room1에 입장 또는 생성
        PhotonNetwork.JoinOrCreateRoom("Room1", new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Photon 서버 연결 해제: " + cause);
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

    // Send 버튼 호출
    public void Send()
    {
        if (_chatInput == null || string.IsNullOrEmpty(_chatInput.text))
        {
            Debug.LogError("Message is empty or chat input is null.");
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("Not in a room. Cannot send message.");
            return;
        }

        string email = FirebaseAuthManager.Instance._userEmail;
        string formattedMessage = $"{email}: {_chatInput.text}";

        _pv.RPC(nameof(ChatRPC), RpcTarget.All, formattedMessage);
        _chatInput.text = "";
    }

    [PunRPC]
    private void ChatRPC(string msg)
    {
        AddChatMessage(msg);
    }

    // 메시지 밀기 로직 통합
    private void AddChatMessage(string msg)
    {
        if (_chatText == null || _chatText.Length == 0)
            return;

        for (int i = 1; i < _chatText.Length; i++)
        {
            _chatText[i - 1].text = _chatText[i].text;
        }
        _chatText[_chatText.Length - 1].text = msg;

        Debug.Log($"Chat message added: {msg}");
    }

    #endregion

    #region GameManager 연동

    public void OnGameStateChange(GameState newGameState)
    {
        if (_pv == null) return;

        if (newGameState == GameState.Playing)
            SendSystemMessage("*** Game Start ***");
        else if (newGameState == GameState.GameOver)
            SendSystemMessage("*** Game Over ***");
    }

    private void SendSystemMessage(string message)
    {
        _pv.RPC(nameof(ChatRPC), RpcTarget.All, $"<color=red>{message}</color>");
    }

    #endregion

    #region UI 활성화/비활성화

    public void OnTextClick()
    {
        SetTextActive(false);
    }

    private void SetTextActive(bool flag)
    {
        if (_textHide != null)
            _textHide.SetActive(flag);
    }

    #endregion

    private void OnDestroy()
    {
        // GameManager 이벤트 구독 해제
        if (GameManager.Instance != null)
            GameManager.Instance._onGameStateChange -= OnGameStateChange;
    }
}
