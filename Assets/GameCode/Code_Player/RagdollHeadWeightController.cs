using UnityEngine;

public class RagdollHeadWeightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody _rootBody;
    [SerializeField] private Rigidbody _headBody;

    [Header("Balance")]
    [SerializeField, Min(0f)] private float _spring = 45f;
    [SerializeField, Min(0f)] private float _damping = 8f;
    [SerializeField, Min(0f)] private float _maxTorque = 90f;

    [Header("Head Weight")]
    [SerializeField, Range(0f, 40f)] private float _pitchFromForwardSpeed = 2.2f;
    [SerializeField, Range(0f, 40f)] private float _yawFromSideSpeed = 2.6f;
    [SerializeField, Range(0f, 10f)] private float _rollFromTurn = 1.8f;
    [SerializeField, Range(0f, 45f)] private float _maxPitch = 18f;
    [SerializeField, Range(0f, 45f)] private float _maxYaw = 16f;
    [SerializeField, Range(0f, 45f)] private float _maxRoll = 14f;

    [Header("Landing Kick")]
    [SerializeField] private bool _enableLandingKick = true;
    [SerializeField, Min(0f)] private float _landingVelocityThreshold = 6f;
    [SerializeField, Min(0f)] private float _landingKickTorque = 18f;

    private Quaternion _baseLocalRotation;
    private float _prevVerticalVelocity;
    private bool _isReady;

    private void Start()
    {
        if (_rootBody == null || _headBody == null)
        {
            Debug.LogWarning("[RagdollHeadWeightController] Assign _rootBody and _headBody.", this);
            return;
        }

        _baseLocalRotation = Quaternion.Inverse(_rootBody.rotation) * _headBody.rotation;
        _prevVerticalVelocity = _rootBody.linearVelocity.y;
        _isReady = true;
    }

    private void FixedUpdate()
    {
        if (!_isReady)
            return;

        Vector3 localVelocity = _rootBody.transform.InverseTransformDirection(_rootBody.linearVelocity);
        Vector3 rootAngular = _rootBody.angularVelocity;

        float pitch = Mathf.Clamp(-localVelocity.z * _pitchFromForwardSpeed, -_maxPitch, _maxPitch);
        float yaw = Mathf.Clamp(localVelocity.x * _yawFromSideSpeed, -_maxYaw, _maxYaw);
        float roll = Mathf.Clamp(-rootAngular.y * Mathf.Rad2Deg * _rollFromTurn, -_maxRoll, _maxRoll);

        Quaternion desiredRotation = _rootBody.rotation * _baseLocalRotation * Quaternion.Euler(pitch, yaw, roll);
        ApplySpringTorque(desiredRotation);
        ApplyLandingKick();

        _prevVerticalVelocity = _rootBody.linearVelocity.y;
    }

    private void ApplySpringTorque(Quaternion desiredRotation)
    {
        Quaternion delta = desiredRotation * Quaternion.Inverse(_headBody.rotation);
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);

        if (float.IsNaN(axis.x) || float.IsNaN(axis.y) || float.IsNaN(axis.z))
            return;

        if (angleDeg > 180f)
            angleDeg -= 360f;

        Vector3 springTorque = axis.normalized * (angleDeg * Mathf.Deg2Rad * _spring);
        Vector3 dampingTorque = _headBody.angularVelocity * _damping;
        Vector3 torque = springTorque - dampingTorque;

        if (torque.sqrMagnitude > _maxTorque * _maxTorque)
            torque = torque.normalized * _maxTorque;

        _headBody.AddTorque(torque, ForceMode.Acceleration);
    }

    private void ApplyLandingKick()
    {
        if (!_enableLandingKick)
            return;

        float vy = _rootBody.linearVelocity.y;
        bool wasFallingFast = _prevVerticalVelocity < -_landingVelocityThreshold;
        bool nowRecovered = vy > -0.4f;

        if (!wasFallingFast || !nowRecovered)
            return;

        Vector3 kickAxis = _headBody.transform.right;
        _headBody.AddTorque(kickAxis * _landingKickTorque, ForceMode.Impulse);
    }
}
