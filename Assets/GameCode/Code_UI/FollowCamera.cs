using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private float _rotationSpeed = 50f;
    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 30f;

    private float _yaw = 0f;
    private float _pitch = 20f;
    private Vector3 _offset;

    void Start()
    {
        if (_playerTransform != null)
        {
            _offset = transform.position - _playerTransform.position;
            _yaw = transform.eulerAngles.y;
        }
        else
        {
            FindLocalPlayer();
        }
    }

    void LateUpdate()
    {
        if (_playerTransform == null) return;

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = _playerTransform.position + (rotation * new Vector3(0, _offset.y, -_offset.magnitude));
        transform.LookAt(_playerTransform.position + Vector3.up * 1.5f);
    }

    public void AddRotation(float yawDelta, float pitchDelta)
    {
        _yaw += yawDelta * _rotationSpeed * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch - pitchDelta * _rotationSpeed * Time.deltaTime, _minPitch, _maxPitch);
    }

    private void FindLocalPlayer()
    {
        if (NetworkAuthorityManager.Instance != null &&
            NetworkAuthorityManager.Instance.LocalSpawnedPlayer != null)
        {
            _playerTransform = NetworkAuthorityManager.Instance.LocalSpawnedPlayer.transform;
            if (_playerTransform != null)
            {
                _offset = transform.position - _playerTransform.position;
                _yaw = transform.eulerAngles.y;
            }
        }
    }
}
