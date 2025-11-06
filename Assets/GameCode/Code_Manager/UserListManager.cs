using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class UserListManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    [SerializeField] private Text[] _userName;               // 플레이어 이름 표시 UI
    [SerializeField] private List<string> _userEmails = new List<string>(); // 접속한 플레이어 닉네임 리스트

    private void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        if (_userName == null || _userName.Length == 0)
            Debug.LogWarning("UserListManager: _userName이 할당되지 않았습니다.");
    }

    #region Photon 콜백 처리

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined the room. Updating player list...");
        UpdateUserList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateUserList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateUserList();
    }

    #endregion

    /// <summary>
    /// 현재 방에 있는 플레이어 정보를 기반으로 UI 및 리스트 갱신
    /// </summary>
    private void UpdateUserList()
    {
        if (_userName == null) return;

        // 기존 리스트 초기화
        _userEmails.Clear();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.NickName != PhotonNetwork.NickName) // 자기 자신 제외
            {
                _userEmails.Add(p.NickName); // 닉네임 추가
            }
        }

        // UI 업데이트
        for (int i = 0; i < _userName.Length; i++)
        {
            if (i < _userEmails.Count)
                _userName[i].text = _userEmails[i];
            else
                _userName[i].text = ""; // 남는 칸 초기화
        }
    }
}