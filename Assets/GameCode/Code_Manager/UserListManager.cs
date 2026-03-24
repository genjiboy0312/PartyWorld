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
    [SerializeField] private bool _includeSelf = true;

    private void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        if (_userName == null || _userName.Length == 0)
            Debug.LogWarning("UserListManager: _userName이 할당되지 않았습니다.");

        // 씬 로드 타이밍상 이미 룸에 들어가 있는 상태로 Start가 호출될 수 있어 초기 1회 갱신
        UpdateUserList();
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

        if (!PhotonNetwork.InRoom)
        {
            for (int i = 0; i < _userName.Length; i++)
            {
                if (_userName[i] != null)
                    _userName[i].text = string.Empty;
            }
            return;
        }

        // 기존 리스트 초기화
        _userEmails.Clear();

        Player[] players = PhotonNetwork.PlayerList;
        System.Array.Sort(players, (a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

        foreach (Player p in players)
        {
            if (!_includeSelf && p == PhotonNetwork.LocalPlayer)
                continue;

            string nick = string.IsNullOrWhiteSpace(p.NickName) ? $"Player_{p.ActorNumber}" : p.NickName;
            _userEmails.Add(nick);
        }

        // UI 업데이트
        for (int i = 0; i < _userName.Length; i++)
        {
            if (i < _userEmails.Count)
                _userName[i].text = _userEmails[i];
            else
                _userName[i].text = ""; // 남는 칸 초기화
        }

        Debug.Log($"[UserListManager] UpdateUserList (count={_userEmails.Count}, room={PhotonNetwork.CurrentRoom?.Name})");
    }
}
