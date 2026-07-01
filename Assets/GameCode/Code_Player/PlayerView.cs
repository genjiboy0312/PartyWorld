// PlayerView.cs
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerView : MonoBehaviour
{
    public Rigidbody Rigidbody { get; private set; }
    public Animator Animator { get; private set; }

    private Vector3 _targetVelocity;
    private float _rotationSpeed = 15f;
    private Coroutine _currentDiveCoroutine;
    private WaitForFixedUpdate _waitForFixedUpdate;
    private bool _isDiving;
    private Vector3 _diveDirection;

    // Model 참조 추가
    private PlayerModel _model;

    private static readonly int IsWalkParam = Animator.StringToHash("isWalk");
    private static readonly int MoveSpeedParam = Animator.StringToHash("moveSpeed");
    private bool _hasIsWalkParam;
    private bool _hasMoveSpeedParam;

    public bool IsDiving => _isDiving;

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Animator = GetComponentInChildren<Animator>();

        //  혹시 모를 NavMeshAgent 제거
        var _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (_agent != null)
            Destroy(_agent);

        if (Animator != null)
        {
            Animator.applyRootMotion = false;
            CacheAnimatorParams();
        }

        Rigidbody.useGravity = true;
        Rigidbody.linearDamping = 0f;
        Rigidbody.mass = 1f;
        Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        _waitForFixedUpdate = new WaitForFixedUpdate();
    }

    // Model 설정 메서드 추가
    public void SetModel(PlayerModel model)
    {
        _model = model;
    }

    private void Update()
    {
        if (!_isDiving && _targetVelocity.sqrMagnitude > 0.01f)
        {
            var velocity = Rigidbody.linearVelocity;
            velocity.x = _targetVelocity.x;
            velocity.z = _targetVelocity.z;
            Rigidbody.linearVelocity = velocity;
        }

        UpdateLocomotionAnimator();
    }

    public void Move(Vector3 velocity)
    {
        _targetVelocity = velocity;
    }

    public void LookAt(Vector3 direction)
    {
        if (_isDiving || direction == Vector3.zero)
            return;

        Quaternion _targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _targetRot,
            Time.deltaTime * _rotationSpeed
        );
    }

    public void Jump(float jumpPower)
    {
        var _velocity = Rigidbody.linearVelocity;
        _velocity.y = jumpPower;
        Rigidbody.linearVelocity = _velocity;

        Animator.SetTrigger("doJump");
    }

    public void Dive(Vector3 direction, float force)
    {
        if (_currentDiveCoroutine != null)
            StopCoroutine(_currentDiveCoroutine);

        _isDiving = true;
        _diveDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        Animator.SetTrigger("doDive");

        Rigidbody.useGravity = false;
        Rigidbody.linearVelocity = Vector3.zero;
        Rigidbody.AddForce(_diveDirection * force * 5f, ForceMode.Acceleration);
        _targetVelocity = Vector3.zero;

        _currentDiveCoroutine = StartCoroutine(DiveRoutine());

        Debug.Log($"Dive 시작! 방향: {_diveDirection}, 힘: {force}");
    }
    public void DashAnimation()
    {
        Animator.SetTrigger("doDash");
    }
    public void Grap()
    {

        Debug.Log($"{gameObject.name} 잡음");
    }
    private IEnumerator DiveRoutine()
    {
        float _duration = 0.6f;
        float _elapsed = 0f;
        float _holdTime = 0.35f;

        while (_elapsed < _duration)
        {
            if (_elapsed < _holdTime)
            {
                // AddForce 점진 가속 — 부드러운 전진
                Rigidbody.AddForce(_diveDirection * 100f, ForceMode.Acceleration);
            }
            else
            {
                // 저항력으로 부드러운 감속
                Vector3 planarVel = new Vector3(Rigidbody.linearVelocity.x, 0f, Rigidbody.linearVelocity.z);
                Rigidbody.AddForce(-planarVel * 2f, ForceMode.Acceleration);
            }
            // Y축 속도 고정 (중력 끈 상태)
            Rigidbody.linearVelocity = new Vector3(Rigidbody.linearVelocity.x, 0f, Rigidbody.linearVelocity.z);

            _elapsed += Time.fixedDeltaTime;
            yield return _waitForFixedUpdate;
        }

        // Dive 종료 처리
        Rigidbody.useGravity = true;
        _isDiving = false;
        _currentDiveCoroutine = null;

        if (_model != null)
            _model.IsDive = false;

        Debug.Log("Dive 종료! Model.IsDive = false");
    }
    public void StopMovement()
    {
        _targetVelocity = Vector3.zero;
        Rigidbody.linearVelocity = Vector3.zero;
        Animator.SetBool("isMove", false);

        if (_currentDiveCoroutine != null)
        {
            StopCoroutine(_currentDiveCoroutine);
            _currentDiveCoroutine = null;
            _isDiving = false;
            Rigidbody.useGravity = true;

            // Model 상태도 초기화
            if (_model != null)
                _model.IsDive = false;
        }
    }

    public void ResetDiveState()
    {
        _isDiving = false;
        if (_currentDiveCoroutine != null)
        {
            StopCoroutine(_currentDiveCoroutine);
            _currentDiveCoroutine = null;
        }
        Rigidbody.useGravity = true;

        if (_model != null)
        {
            _model.IsDive = false;
        }

        Debug.Log("Dive 상태 강제 리셋!");
    }

    private void OnDestroy()
    {
        if (_currentDiveCoroutine != null)
        {
            StopCoroutine(_currentDiveCoroutine);
        }
    }

    private void CacheAnimatorParams()
    {
        // Animator 파라미터 존재 여부를 캐싱(없는 파라미터 Set 시 경고 방지)
        if (Animator == null)
            return;

        foreach (AnimatorControllerParameter p in Animator.parameters)
        {
            if (p.nameHash == IsWalkParam)
                _hasIsWalkParam = true;
            else if (p.nameHash == MoveSpeedParam)
                _hasMoveSpeedParam = true;
        }
    }

    private void UpdateLocomotionAnimator()
    {
        // 조인트/물리 이동을 사용해도 애니메이션이 따라가도록 실제 속도로 판단
        if (Animator == null || Rigidbody == null)
            return;

        Vector3 v = Rigidbody.linearVelocity;
        Vector3 planar = new Vector3(v.x, 0f, v.z);
        float speed = planar.magnitude;
        bool isMoving = speed > 0.1f; // 기준치 조정

        if (_hasIsWalkParam)
            Animator.SetBool(IsWalkParam, isMoving);

        if (_hasMoveSpeedParam)
            Animator.SetFloat(MoveSpeedParam, speed);
    }
}
