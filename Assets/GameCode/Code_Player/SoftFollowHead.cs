using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SoftFollowHead : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _target;        // 목(Neck) 또는 Chest
    [SerializeField] private Rigidbody _rootRb;        // 캐릭터의 중심 리지드바디

    [Header("Settings")]
    [SerializeField] private float _torqueForce = 400f;
    [SerializeField] private float _damping = 5f;
    [SerializeField] private float _inertiaFactor = 12f;
    [SerializeField] private float _maxBackAngle = 45f;

    private Rigidbody _rb;
    private Vector3 _prevVelocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _prevVelocity = _rootRb ? _rootRb.velocity : Vector3.zero;
    }

    void FixedUpdate()
    {
        if (_target == null || _rootRb == null)
            return;

        // ------------------------------
        // [1] 기본 자세 복원 회전
        // ------------------------------
        Quaternion delta = _rb.rotation * Quaternion.Inverse(_target.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;

        if (Mathf.Abs(angle) > 0.01f)
        {
            axis = transform.TransformDirection(axis);
            Vector3 torque = axis * angle * _torqueForce - _rb.angularVelocity * _damping;
            _rb.AddTorque(torque, ForceMode.Acceleration);
        }

        // ------------------------------
        // [2] 관성 기반 뒤로 젖힘
        // ------------------------------
        Vector3 acceleration = (_rootRb.velocity - _prevVelocity) / Time.fixedDeltaTime;
        _prevVelocity = _rootRb.velocity;

        float accelMag = acceleration.magnitude;

        // 정지 시 관성 제거
        if (accelMag < 0.05f)
            return;

        // 전진 방향 가속도만 영향받게 (좌/우/후진 무시)
        float forwardDot = Vector3.Dot(_rootRb.transform.forward, acceleration.normalized);
        forwardDot = Mathf.Clamp(forwardDot, 0f, 1f); // 전진할 때만 머리 뒤로

        // 뒤로 젖힐 최대 각도 제한
        float currentLocalX = NormalizeAngle(transform.localEulerAngles.x);
        if (currentLocalX < -_maxBackAngle)
            return;

        // 뒤로 젖히는 힘 = 로컬 X축 기준 토크
        float tilt = forwardDot * _inertiaFactor;
        Vector3 inertialTorque = -transform.right * tilt;

        _rb.AddTorque(inertialTorque, ForceMode.Acceleration);

#if UNITY_EDITOR
        Debug.DrawRay(transform.position, axis * 0.3f, Color.red);
        Debug.DrawRay(transform.position, -acceleration.normalized * 0.5f, Color.blue);
        Debug.Log($"angle={angle:F2}, accel={accelMag:F2}, forward={forwardDot:F2}, localX={currentLocalX:F2}");
#endif
    }

    // Unity LocalEulerAngles 보정
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
