using UnityEngine;

/// <summary>
/// StartLine의 장벽(Collider)을 제어합니다.
/// 초기에는 물리벽(isTrigger=false) 상태로 플레이어를 차단하고,
/// OpenBarrier() 호출 시 Collider를 비활성화하여 통과를 허용합니다.
/// </summary>
public class StartLineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider _barrierCollider;
    [SerializeField] private GameObject _visualBarrier;

    private void Awake()
    {
        if (_barrierCollider == null)
            _barrierCollider = GetComponent<Collider>();

        // 초기 상태: 단단한 벽 (Trigger OFF)
        _barrierCollider.isTrigger = false;
    }

    /// <summary>레이스 시작 시 장벽을 열어 통과를 허용합니다.</summary>
    public void OpenBarrier()
    {
        _barrierCollider.enabled = false;

        if (_visualBarrier != null)
            _visualBarrier.SetActive(false);

        Debug.Log("[StartLineController] Barrier opened.");
    }

    /// <summary>장벽을 다시 닫습니다 (리셋/재시작용).</summary>
    public void CloseBarrier()
    {
        _barrierCollider.enabled = true;
        _barrierCollider.isTrigger = false;

        if (_visualBarrier != null)
            _visualBarrier.SetActive(true);

        Debug.Log("[StartLineController] Barrier closed.");
    }

    /// <summary>에디터 재생 중 리셋 편의 메서드 (OnDisable 연동 없이 직접 호출).</summary>
    public void ResetBarrier()
    {
        CloseBarrier();
    }
}
