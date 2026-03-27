using UnityEngine;
using Photon.Pun;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Vector3 _offset = new Vector3(0, 5, -7);
    [SerializeField] private Vector3 _rotationOffset = new Vector3(20, 0, 0);
    [SerializeField] private float _smoothTime = 0.15f;
    [SerializeField] private float _velocityThreshold = 0.5f;

    private Vector3 _velocity;
    private float _rotationVelocity;
    private float _lastFacingY;

    void LateUpdate()
    {
        if (_playerTransform == null)
            FindLocalPlayer();

        if (_playerTransform == null)
            return;

        // 위치: 플레이어 기준 오프셋 + 부드러운 보간
        Vector3 targetPos = _playerTransform.position + _playerTransform.rotation * _offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);

        // 회전: 플레이어가 실제로 바라보는 방향 기준
        float targetY = GetFacingAngle();
        float currentY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetY, ref _rotationVelocity, _smoothTime);
        transform.rotation = Quaternion.Euler(_rotationOffset.x, currentY, _rotationOffset.z);
    }

    private float GetFacingAngle()
    {
        Rigidbody rb = _playerTransform.GetComponentInChildren<Rigidbody>();
        if (rb != null && rb.linearVelocity.magnitude > _velocityThreshold)
        {
            _lastFacingY = Quaternion.LookRotation(new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z)).eulerAngles.y;
        }
        return _lastFacingY;
    }

    private void FindLocalPlayer()
    {
        if (NetworkAuthorityManager.Instance != null &&
            NetworkAuthorityManager.Instance.LocalSpawnedPlayer != null)
        {
            _playerTransform = NetworkAuthorityManager.Instance.LocalSpawnedPlayer.transform;
            Rigidbody rb = _playerTransform.GetComponentInChildren<Rigidbody>();
            if (rb != null)
                _lastFacingY = _playerTransform.eulerAngles.y;
            return;
        }

        PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        for (int i = 0; i < views.Length; i++)
        {
            if (views[i].IsMine && views[i].GetComponent<Rigidbody>() != null)
            {
                _playerTransform = views[i].transform;
                _lastFacingY = _playerTransform.eulerAngles.y;
                return;
            }
        }
    }
}
