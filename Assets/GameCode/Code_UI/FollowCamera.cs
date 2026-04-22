using UnityEngine;
using Photon.Pun;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Vector3 _offset = new Vector3(0, 5, -7);
    [SerializeField] private Vector3 _rotationOffset = new Vector3(20, 0, 0);
    [SerializeField] private float _smoothTime = 0.15f;

    private Vector3 _velocity;
    private float _rotationVelocity;
    private float _lastFacingY;

    void LateUpdate()
    {
        if (_playerTransform == null)
            FindLocalPlayer();

        if (_playerTransform == null)
            return;

        // 회전: 플레이어의 수평(yaw) 방향 기준으로만 갱신
        float targetY = GetFacingAngle();
        float currentY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetY, ref _rotationVelocity, _smoothTime);

        // 위치: yaw 기준 오프셋만 적용해서 pitch/roll 틀어짐 방지
        Quaternion yawRotation = Quaternion.Euler(0f, currentY, 0f);
        Vector3 targetPos = _playerTransform.position + yawRotation * _offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);

        transform.rotation = Quaternion.Euler(_rotationOffset.x, currentY, _rotationOffset.z);
    }

    private float GetFacingAngle()
    {
        Vector3 horizontalForward = Vector3.ProjectOnPlane(_playerTransform.forward, Vector3.up);
        if (horizontalForward.sqrMagnitude > 0.0001f)
        {
            _lastFacingY = Quaternion.LookRotation(horizontalForward, Vector3.up).eulerAngles.y;
        }
        return _lastFacingY;
    }

    private void FindLocalPlayer()
    {
        if (NetworkAuthorityManager.Instance != null &&
            NetworkAuthorityManager.Instance.LocalSpawnedPlayer != null)
        {
            _playerTransform = NetworkAuthorityManager.Instance.LocalSpawnedPlayer.transform;
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
