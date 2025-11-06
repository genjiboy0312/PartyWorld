using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SoftFollowHead : MonoBehaviour
{
    public Transform target;        // Neck 기준
    public Rigidbody rootRb;        // 캐릭터의 메인 리지드바디 (몸통)
    public float torqueForce = 400f; // 복원 강도
    public float damping = 5f;       // 감쇠
    public float inertiaFactor = 8f; // 관성 토크 강도
    public float maxAngle = 45f;

    Rigidbody rb;
    Vector3 prevVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rootRb == null)
        {
            // 자동으로 상위에서 찾아봄
            rootRb = GetComponentInParent<Rigidbody>();
        }
        prevVelocity = rootRb ? rootRb.velocity : Vector3.zero;
    }

    void FixedUpdate()
    {
        if (target == null || rootRb == null) return;

        // 회전 복원 토크
        Quaternion deltaRot = rb.rotation * Quaternion.Inverse(target.rotation);
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;
        if (Mathf.Abs(angle) > 0.01f)
        {
            axis = transform.TransformDirection(axis);
            Vector3 torque = axis * angle * torqueForce - rb.angularVelocity * damping;
            rb.AddTorque(torque, ForceMode.Acceleration);
        }

        // --- 관성 반응 추가 (몸의 가속도 방향 반대쪽으로 머리 젖힘) ---
        Vector3 acceleration = (rootRb.velocity - prevVelocity) / Time.fixedDeltaTime;
        prevVelocity = rootRb.velocity;

        // 가속의 반대 방향으로 머리를 젖히는 힘
        Vector3 inertialTorque = Vector3.Cross(transform.forward, acceleration.normalized) * inertiaFactor;
        rb.AddTorque(inertialTorque, ForceMode.Acceleration);

#if UNITY_EDITOR
        Debug.DrawRay(transform.position, axis * 0.3f, Color.red);      // 복원 축
        Debug.DrawRay(transform.position, -acceleration.normalized * 0.5f, Color.blue); // 관성 방향
        Debug.Log($"angle={angle:F2}, accel={acceleration.magnitude:F2}");
#endif
    }
}
