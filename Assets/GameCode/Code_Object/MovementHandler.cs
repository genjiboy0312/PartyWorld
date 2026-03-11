using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementHandler : MonoBehaviour
{
    [SerializeField] private List<GameObject> _moventPoints;        //  이동 포인트
    [SerializeField] private float _moveSpeed = 5f;                 //  이동 속도
    [SerializeField] private bool _isMoving = false;                //  이동 중 여부 체크
    void Start()
    {
        if (_moventPoints == null || _moventPoints.Count == 0)
        {
            Debug.LogWarning("Movement points are not set.");
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        //  플레이어랑 부딪히면
        if (collider.CompareTag("Player"))
        {
            StartCoroutine(MovePlayerToWaypoints(collider.transform));
        }
    }
    private IEnumerator MovePlayerToWaypoints(Transform player)
    {
        Rigidbody _playerRb = player.GetComponent<Rigidbody>();
        if (_playerRb != null)
        {
            _playerRb.useGravity = false; // 중력 비활성화
            _playerRb.linearVelocity = Vector3.zero; // 현재 속도 초기화
        }

        _isMoving = true; // 이동 중 상태 설정

        // 각 Waypoint로 이동
        foreach (var point in _moventPoints)
        {
            // 플레이어가 목표 지점에 도달할 때까지 반복
            while (Vector3.Distance(player.position, point.transform.position) > 0.1f)
            {
                // 플레이어 위치를 목표 지점으로 이동
                player.position = Vector3.MoveTowards(player.position, point.transform.position, _moveSpeed * Time.deltaTime);

                // 이동 중 회전 처리 (옵션)
                Vector3 direction = (point.transform.position - player.position).normalized;
                if (direction != Vector3.zero)
                {
                    player.rotation = Quaternion.Slerp(player.rotation, Quaternion.LookRotation(direction), Time.deltaTime * _moveSpeed);
                }

                yield return null; // 다음 프레임까지 대기
            }
        }

        _isMoving = false; // 이동 완료
        if (_playerRb != null)
        {
            _playerRb.useGravity = true; // 중력 활성화
        }
    }
}
