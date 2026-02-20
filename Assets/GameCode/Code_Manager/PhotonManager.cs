using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    private readonly string _version = "1.0f";                                      //  버전 입력
    [SerializeField] private string _userId = "GenJiBoy";                           //  사용자 아이디 입력

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;                                //  같은 룸의 유저들에게 자동으로 씬을 로딩
        PhotonNetwork.GameVersion = _version;                                       //  같은 버전의 유저끼리 접속 허용
        PhotonNetwork.NickName = _userId;                                           //  유저 아이디 할당

        // SendRate 및 SerializationRate 설정
        PhotonNetwork.SendRate = 60; // 초당 전송되는 네트워크 패킷의 수
        PhotonNetwork.SerializationRate = 60; // 초당 전송되는 시리얼라이즈된 패킷의 수
        Debug.Log("SendRate : " + PhotonNetwork.SendRate);                                          //  통신 횟수 설정

        PhotonNetwork.ConnectUsingSettings();                                       //  서버 접속
    }

    //  포톤 서버에 접속 후 호출되는 CallBack Method
    public override void OnConnectedToMaster()
    {
        //PhotonNetwork.JoinOrCreateRoom("Joint Or CreateRoom", new RoomOptions { IsOpen = true }, TypedLobby.Default);
        Debug.Log($"PhotonNetwork.InLobby = {PhotonNetwork.InLobby}");
        PhotonNetwork.JoinLobby();                              //  로비 입장
    }

    //  로비에 접속 후 호출 되는 Callback Method
    public override void OnJoinedLobby()
    {
        Debug.Log($"PhotonNetwork.InLobby = {PhotonNetwork.InLobby}");
        PhotonNetwork.JoinRandomRoom();                         //  랜덤 매치 메이킹 기능 제공
    }

    //  랜덤한 룸 입장이 실패했을 경우 호출되는 Callback Method
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // 실패시 띄우는 디버그
        Debug.Log($"JoinRandom Filed => {returnCode} : {message}");

        // 룸의 속성 정의
        RoomOptions _roomOps = new RoomOptions();
        _roomOps.MaxPlayers = 10;                       //  무료는 최대 20
        _roomOps.IsOpen = true;                         //  룸의 오픈 여부
        _roomOps.IsVisible = true;                      //  로비에서 룸 목록에 노출 시킬지 여부

        // 룸 생성
        PhotonNetwork.CreateRoom("My Room", _roomOps);
    }

    // 룸 생성이 완료된 후 호출되는 Callback Method
    public override void OnCreatedRoom()
    {
        Debug.Log("*** Created Room! ***");
        Debug.Log($"Room Name = {PhotonNetwork.CurrentRoom.Name}");
    }
    // 룸에 입장한 후 호출되는 Callback Method
    public override void OnJoinedRoom()
    {
        Debug.Log($"PhotonNetwork.InRoom : {PhotonNetwork.InRoom}");
        Debug.Log($"Player Count : {PhotonNetwork.CurrentRoom.PlayerCount}");

        //  룸에 접속한 사용자 정보 확인
        foreach (var _player in PhotonNetwork.CurrentRoom.Players)
        {
            Debug.Log($"{_player.Value.NickName}, {_player.Value.ActorNumber}");
        }

        // Player 소환
        Spawn();
    }

    // Player 소환하는 함수
    public void Spawn()
    {
        PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
    }

    //public override void OnJoinedRoom()
    //{
    //    PhotonNetwork.Instantiate("GenJiBoy", Vector3.zero, Quaternion.identity, 0);

    //}
    public void StartGame()
    {
        Debug.Log("PhotonManager에서 게임 시작 처리");
        // 게임 시작 시 처리할 로직 추가
    }

    public void EndGame()
    {
        Debug.Log("PhotonManager에서 게임 오버 처리");
        // 게임 오버 시 처리할 로직 추가
    }
}
