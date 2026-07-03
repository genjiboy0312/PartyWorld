using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

[DisallowMultipleComponent]
public sealed class LobbyCountdownManager : MonoBehaviour
{
    private const string ROOM_PROP_COUNTDOWN_ACTIVE = "lobbyCountdownActive";
    private const string ROOM_PROP_COUNTDOWN_START_TIME = "lobbyCountdownStartTime";
    private const string ROOM_PROP_COUNTDOWN_DURATION = "lobbyCountdownDuration";

    [Header("Lobby Countdown")]
    [SerializeField] private float _lobbyCountdownSeconds = 5f;

    private NetworkAuthorityManager _authority;
    private SceneTransitionManager _sceneTransition;
    private bool _issuedLoadingForThisCountdown;

    internal void Configure(NetworkAuthorityManager authority, SceneTransitionManager sceneTransition, float lobbyCountdownSeconds)
    {
        _authority = authority;
        _sceneTransition = sceneTransition;
        _lobbyCountdownSeconds = lobbyCountdownSeconds;
    }

    internal void EvaluateLobbyCountdown()
    {
        // 룸 로비에서 전원 Ready면 카운트다운을 시작하고, 깨지면 즉시 취소
        if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        bool allReady = _authority != null && _authority.AreAllPlayersReady();

        if (!allReady)
        {
            StopLobbyCountdown();
            return;
        }

        StartLobbyCountdownIfNeeded();
    }

    internal void StartLobbyCountdownIfNeeded()
    {
        // 이미 카운트다운이 켜져 있으면 중복 시작하지 않음
        if (IsLobbyCountdownActive())
            return;

        _issuedLoadingForThisCountdown = false;

        PhotonHashtable props = new PhotonHashtable
        {
            { ROOM_PROP_COUNTDOWN_ACTIVE, true },
            { ROOM_PROP_COUNTDOWN_START_TIME, PhotonNetwork.Time },
            { ROOM_PROP_COUNTDOWN_DURATION, _lobbyCountdownSeconds }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    internal void StopLobbyCountdown()
    {
        // 카운트다운을 끄고, 다음 Ready 조합에서 다시 시작 가능하게 리셋
        if (!IsLobbyCountdownActive())
            return;

        _issuedLoadingForThisCountdown = false;

        PhotonHashtable props = new PhotonHashtable
        {
            { ROOM_PROP_COUNTDOWN_ACTIVE, false }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    internal bool IsLobbyCountdownActive()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return false;

        PhotonHashtable props = PhotonNetwork.CurrentRoom.CustomProperties as PhotonHashtable;
        if (props == null)
            return false;

        return props.TryGetValue(ROOM_PROP_COUNTDOWN_ACTIVE, out object raw) && raw is bool active && active;
    }

    internal bool TryGetLobbyCountdownRemaining(out float remainingSeconds, out bool isActive)
    {
        remainingSeconds = 0f;
        isActive = false;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return false;

        PhotonHashtable props = PhotonNetwork.CurrentRoom.CustomProperties as PhotonHashtable;
        if (props == null)
            return false;

        if (!props.TryGetValue(ROOM_PROP_COUNTDOWN_ACTIVE, out object activeRaw) || activeRaw is not bool active)
            return false;

        if (!props.TryGetValue(ROOM_PROP_COUNTDOWN_START_TIME, out object startRaw) || startRaw is not double startTime)
            return false;

        if (!props.TryGetValue(ROOM_PROP_COUNTDOWN_DURATION, out object durRaw))
            return false;

        float durationSeconds = durRaw switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            _ => 0f
        };

        isActive = active;
        if (!isActive)
            return true;

        double now = PhotonNetwork.Time;
        remainingSeconds = Mathf.Max(0f, durationSeconds - (float)(now - startTime));
        return true;
    }

    private void Update()
    {
        // 마스터는 카운트다운 만료 시 로딩 씬으로 전환(한 번만)
        if (_authority == null || _sceneTransition == null)
            return;

        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        string currentScene = SceneManager.GetActiveScene().name;
        if (!_sceneTransition.IsLobbyScene(currentScene))
            return;

        if (_issuedLoadingForThisCountdown)
            return;

        if (!TryGetLobbyCountdownRemaining(out float remaining, out bool active))
            return;

        if (!active)
            return;

        if (remaining > 0f)
            return;

        if (!_authority.AreAllPlayersReady())
        {
            StopLobbyCountdown();
            return;
        }

        _issuedLoadingForThisCountdown = true;
        _sceneTransition.SelectAndStoreRandomMap();
        PhotonNetwork.CurrentRoom.IsOpen = false;
        _sceneTransition.LoadLoadingScene();
    }
}
