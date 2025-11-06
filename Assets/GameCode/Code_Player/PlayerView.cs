using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerView : MonoBehaviour
{
    public Rigidbody Rigidbody { get; private set; }
    public Animator Animator { get; private set; }

    private Vector3 _targetVelocity;
    private float _rotationSpeed = 15f; // 회전 부드럽게 보간용

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Animator = GetComponentInChildren<Animator>();

        // 물리 기반 이동에 불필요한 NavMesh 제거
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            Destroy(agent);

        // 루트 모션 강제 비활성화
        if (Animator != null)
            Animator.applyRootMotion = false;

        // 물리 속성 기본값 설정
        Rigidbody.useGravity = true;
        Rigidbody.drag = 0f;
        Rigidbody.mass = 1f;
        Rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 물리 이동 보간으로 부드럽게
    }

    private void FixedUpdate()
    {
        // FixedUpdate에서 물리 이동
        if (_targetVelocity != Vector3.zero)
        {
            Rigidbody.velocity = new Vector3(_targetVelocity.x, Rigidbody.velocity.y, _targetVelocity.z);
        }

        // 이동 여부 애니메이션 반영
        Animator.SetBool("isMove", _targetVelocity.sqrMagnitude > 0.01f);
    }

    public void Move(Vector3 velocity)
    {
        // Update()에서 받은 이동 벡터를 FixedUpdate()에서 실제 적용하도록 저장
        _targetVelocity = velocity;
    }

    public void LookAt(Vector3 direction)
    {
        if (direction == Vector3.zero)
            return;

        // 즉각 회전 대신 부드럽게 보간
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _rotationSpeed);
    }

    public void Jump(float jumpPower)
    {
        // 수직 방향 속도만 덮어쓰기
        var v = Rigidbody.velocity;
        v.y = jumpPower;
        Rigidbody.velocity = v;

        Animator.SetTrigger("doJump");
    }

    public void Dash()
    {
        Animator.SetTrigger("doDash");
    }

    public void StopMovement()
    {
        _targetVelocity = Vector3.zero;
        Rigidbody.velocity = Vector3.zero;
        Animator.SetBool("isMove", false);
    }
}
