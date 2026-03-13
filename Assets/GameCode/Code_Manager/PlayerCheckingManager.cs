using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCheckingManager : MonoBehaviourPunCallbacks
{
    [Header("Legacy")]
    [SerializeField] private bool _useLegacyFlow = false;

    [SerializeField] private List<Scene> _mapScnene;
    [SerializeField] private List<int> _sceneIdx;                           // 씬 인덱스를 담는 리스트
    [SerializeField] private string _sceneName;                             // 현재 씬 이름

    [SerializeField] private List<bool> _checkingPlayers;
    [SerializeField] private int _playerCount;                              // 방에 접속한 플레이어 수
    [SerializeField] private int _readyCount;                               // 준비 완료한 플레이어 수

    [Header("Setting Loading")]
    [SerializeField] private bool _isLoadingStarted = false;
    [SerializeField] private float _loading;
    [SerializeField] private float _loadingSpeed = 0.001f;                     // 로딩 속도
    [SerializeField] private float _loadingFinish = 100.0f;                    //  로딩 끝
    private void Awake()
    {
        if (!_useLegacyFlow)
            return;
    }

    void Update()
    {
        if (!_useLegacyFlow)
            return;

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        //if (PhotonNetwork.IsMasterClient)
        {
            _playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            //_checkingPlayers = new List<bool>(new bool[_playerCount]);          // 플레이어 수만큼 리스트 초기화
            CheckingPlayersSize();

            if (!_isLoadingStarted)
            {
                _isLoadingStarted = true; // 중복 실행 방지
                StartCoroutine(PlayerLoading());
            }
        }
        //  해당 씬으로 플레이어가 들어왔는지 체크
        if (PhotonNetwork.IsMasterClient && _readyCount == _playerCount)
        {
            //PlayStart(); // 모든 플레이어가 준비되면 게임 시작
            //GoToRandomScene();

            Debug.Log("**********************");
        }

        if(_readyCount == _playerCount)
        {
            Debug.Log("testestestesetest");
        }
    }

    [PunRPC]
    //  로비에 있던 플레이어가 다 체크되면 다음 씬으로 넘어감
    private void PlayStart()
    {
        Debug.Log("All players are ready. Loading next scene...");
        System.Console.WriteLine("checking : " + FirebaseAuthManager.Instance._userEmail);     //  테스트용 디버그

        if (PhotonNetwork.IsMasterClient)
        {
            // 원하는 맵 씬 로드 (예: 첫 번째 맵)
            PhotonNetwork.LoadLevel("Scene_Map01");
        }
    }

    [PunRPC]
    // 로딩  코루틴
    private IEnumerator PlayerLoading()
    {
        while (_loading < _loadingFinish)
        {
            _loading += _loadingSpeed; // 로딩 속도만큼 증가
            //Debug.Log($"현재 로딩 상태: {_loading}%");

            // 프레임마다 대기
            yield return null; 
        }

        // 자신의 준비 상태를 true로 설정
        if (_loading >= _loadingFinish)
        {
            SetMyPlayerReady();
        }
    }
    // _checkingPlayers 크기 조정 함수
    private void CheckingPlayersSize()
    {
        if (_checkingPlayers == null)
        {
            _checkingPlayers = new List<bool>();
        }

        // 플레이어 수가 리스트 크기보다 크면 false 추가
        while (_checkingPlayers.Count < _playerCount)
        {
            _checkingPlayers.Add(false);
        }

        // 플레이어 수가 리스트 크기보다 작으면 뒤에서부터 제거
        while (_checkingPlayers.Count > _playerCount)
        {
            _checkingPlayers.RemoveAt(_checkingPlayers.Count - 1);
        }
        Debug.Log($"_checkingPlayers 상태: {string.Join(", ", _checkingPlayers)}");
    }
    [PunRPC]
    // 자신의 준비 상태를 설정하는 메소드
    private void SetMyPlayerReady()
    {
        if (_checkingPlayers == null || _checkingPlayers.Count == 0)
        {
            Debug.LogError("CheckingPlayers 리스트가 초기화되지 않았습니다!");
            return;
        }

        int _playerIdx = PhotonNetwork.LocalPlayer.ActorNumber - 1; // 자신의 인덱스 계산
        if (_playerIdx >= 0 && _playerIdx < _checkingPlayers.Count)
        {
            _checkingPlayers[_playerIdx] = true; // 자신의 상태를 true로 설정
            Debug.Log($"SetMyPlayerReady called by {PhotonNetwork.LocalPlayer.NickName}");
            Debug.Log($"현재 CheckingPlayers 상태: {_playerIdx} {string.Join(", ", _checkingPlayers)}");

            _readyCount++;
        }
        else
        {
            Debug.LogError("유효하지 않은 플레이어 인덱스입니다!");
        }
    }

    //  랜덤 씬으로 보내는 함수
    public void GoToRandomScene()
    {
        //  Scene Null 이면 Return
        if (_sceneIdx.Count == 0)
        {
            Debug.LogError(" *** Scene Null !! *** ");
            return;
        }

        // 랜덤으로 씬 인덱스를 선택
        int _randIdx = Random.Range(0, _sceneIdx.Count);
        int _selectedSceneIdx = _sceneIdx[_randIdx];

        // 선택된 씬의 이름을 가져옴
        _sceneName = SceneUtility.GetScenePathByBuildIndex(_selectedSceneIdx);

        // 선택된 씬 로드
        SceneManager.LoadScene(_selectedSceneIdx);

        // 씬 이름을 콘솔에 출력
        Debug.Log(" Scene Name : " + _sceneName);
    }


    #region Photon 동기화 코드
    // 새로운 플레이어가 방에 들어왔을 때
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!_useLegacyFlow)
            return;

        _playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"{newPlayer.NickName} has joined. Total players: {_playerCount}");
        Debug.Log($"{FirebaseAuthManager.Instance._userEmail} has joined. Total players: {FirebaseAuthManager.Instance._userEmail.Length}");

        if (PhotonNetwork.IsMasterClient)
        {
            _checkingPlayers.Add(false); // 새 플레이어 추가 (준비 상태는 false)
        }
    }

    // 플레이어가 방에서 나갔을 때
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!_useLegacyFlow)
            return;

        _playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        Debug.Log($"{otherPlayer.NickName} has left. Total players: {_playerCount}");

        if (PhotonNetwork.IsMasterClient)
        {
            _checkingPlayers.RemoveAt(otherPlayer.ActorNumber - 1); // 떠난 플레이어 제거
        }
    }
    #endregion
}
