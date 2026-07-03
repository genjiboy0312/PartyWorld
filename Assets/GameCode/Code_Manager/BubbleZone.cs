using UnityEngine;
using Photon.Pun;

/// <summary>
/// Fall Guys 스타일 버블 존
/// - 플레이어가 이 영역에 닿으면 탈락 처리
/// - 바닥 아래에 위치하여 플레이어가 떨어지면 감지
/// </summary>
public class BubbleZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _eliminationDelay = 0.5f;
    [SerializeField] private bool _showDebugZone = true;
    [SerializeField] private Color _debugZoneColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("Effects")]
    [SerializeField] private GameObject _eliminationEffect;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _eliminationSound;

    [Header("Spectate Camera")]
    [SerializeField] private bool _autoSpectateOnElimination = true;

    // 추적 중인 플레이어
    private System.Collections.Generic.Dictionary<GameObject, float> _playersInZone = new System.Collections.Generic.Dictionary<GameObject, float>();

    private void Update()
    {
        // 딜레이 후 탈락 처리
        var keysToRemove = new System.Collections.Generic.List<GameObject>();

        foreach (var kvp in _playersInZone)
        {
            if (kvp.Key == null)
            {
                keysToRemove.Add(kvp.Key);
                continue;
            }

            _playersInZone[kvp.Key] -= Time.deltaTime;

            if (_playersInZone[kvp.Key] <= 0f)
            {
                EliminatePlayer(kvp.Key);
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _playersInZone.Remove(key);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어만 감지
        if (!IsPlayer(other.gameObject))
            return;

        if (!_playersInZone.ContainsKey(other.gameObject))
        {
            _playersInZone[other.gameObject] = _eliminationDelay;
            Debug.Log($"[BubbleZone] Player entered elimination zone: {other.gameObject.name}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 플레이어가 계속 존에 있으면 딜레이 리셋
        if (IsPlayer(other.gameObject))
        {
            if (_playersInZone.ContainsKey(other.gameObject))
            {
                _playersInZone[other.gameObject] = _eliminationDelay;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 플레이어가 존을 벗어나면 추적 해제
        if (IsPlayer(other.gameObject))
        {
            if (_playersInZone.ContainsKey(other.gameObject))
            {
                _playersInZone.Remove(other.gameObject);
                Debug.Log($"[BubbleZone] Player exited elimination zone: {other.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// 플레이어 탈락 처리
    /// </summary>
    private void EliminatePlayer(GameObject player)
    {
        if (player == null)
            return;

        Debug.Log($"[BubbleZone] Player eliminated: {player.name}");

        // 이펙트 재생
        PlayEliminationEffect(player.transform.position);

        // 사운드 재생
        PlayEliminationSound();

        // 마스터에서만 처리
        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
        {
            // HexArenaManager에 알림
            HexArenaManager.Instance?.HandlePlayerEliminated(player);

            // 관전 모드로 전환
            if (_autoSpectateOnElimination)
            {
                EnableSpectateMode(player);
            }

            // 플레이어 오브젝트 비활성화 또는 파괴
            DisableOrDestroyPlayer(player);
        }
        else
        {
            // 마스터에게 탈락 RPC 전송
            // BubbleZone에 PhotonView 컴포넌트가 필요합니다
            // 해결책 1: PhotonView를 GameObject에 추가
            // 해결책 2: 마스터에서 직접 처리 (권장)
            
            // 마스터가 아니면 마스터에게 알리지 않고 처리
            Debug.LogWarning("[BubbleZone] RPC requires PhotonView component on BubbleZone object");
        }
    }

    /// <summary>
    /// 플레이어인지 확인
    /// </summary>
    private bool IsPlayer(GameObject obj)
    {
        // Player 태그 또는 Player 레이어 체크
        if (obj.CompareTag("Player"))
            return true;

        if (obj.layer == LayerMask.NameToLayer("Player"))
            return true;

        // PhotonView가 있고 IsMine이 아니면 다른 플레이어
        PhotonView pv = obj.GetComponent<PhotonView>();
        if (pv != null && !pv.IsMine)
            return true;

        // PlayerPresenter 컴포넌트 체크
        if (obj.GetComponent<PlayerPresenter>() != null)
            return true;

        return false;
    }

    /// <summary>
    /// 탈락 이펙트 재생
    /// </summary>
    private void PlayEliminationEffect(Vector3 position)
    {
        if (_eliminationEffect != null)
        {
            Instantiate(_eliminationEffect, position, Quaternion.identity);
        }
    }

    /// <summary>
    /// 탈락 사운드 재생
    /// </summary>
    private void PlayEliminationSound()
    {
        if (_eliminationSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_eliminationSound);
        }
    }

    /// <summary>
    /// 관전 모드 활성화
    /// </summary>
    private void EnableSpectateMode(GameObject eliminatedPlayer)
    {
        // TODO: 관전 카메라 시스템 연동
        // PlayerPresenter 또는 전용 SpectateManager 호출
        Debug.Log($"[BubbleZone] Switching to spectate mode for eliminated player");
    }

    /// <summary>
    /// 플레이어 오브젝트 비활성화 또는 파괴
    /// </summary>
    private void DisableOrDestroyPlayer(GameObject player)
    {
        if (player == null)
            return;

        PhotonView pv = player.GetComponent<PhotonView>();

        if (pv != null && PhotonNetwork.InRoom)
        {
            // Photon 네트워크 환경
            if (pv.IsMine)
            {
                // 로컬 플레이어면 비활성화
                player.SetActive(false);
            }
            else
            {
                // 다른 플레이어면 마스터가 파괴
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(player);
                }
            }
        }
        else
        {
            // 비네트워크 환경
            Destroy(player);
        }
    }

    #region Photon RPC

    [PunRPC]
    private void RPC_PlayerEliminated(int playerViewId)
    {
        PhotonView pv = PhotonView.Find(playerViewId);
        if (pv != null)
        {
            GameObject player = pv.gameObject;
            HexArenaManager.Instance?.HandlePlayerEliminated(player);
            DisableOrDestroyPlayer(player);
        }
    }

    #endregion

    #region Gizmos (에디터에서 존 시각화)

    private void OnDrawGizmos()
    {
        if (!_showDebugZone)
            return;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = _debugZoneColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }

    #endregion
}
