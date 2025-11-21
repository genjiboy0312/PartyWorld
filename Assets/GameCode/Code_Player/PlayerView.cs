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

    // Model 참조 추가
    private PlayerModel _model;

    public bool IsDiving => _isDiving;

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Animator = GetComponentInChildren<Animator>();

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) Destroy(agent);

        if (Animator != null) Animator.applyRootMotion = false;

        Rigidbody.useGravity = true;
        Rigidbody.drag = 0f;
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
            var velocity = Rigidbody.velocity;
            velocity.x = _targetVelocity.x;
            velocity.z = _targetVelocity.z;
            Rigidbody.velocity = velocity;
        }

        Animator.SetBool("isWalk", _targetVelocity.sqrMagnitude > 0.01f);
    }

    public void Move(Vector3 velocity)
    {
        _targetVelocity = velocity;
    }

    public void LookAt(Vector3 direction)
    {
        if (_isDiving || direction == Vector3.zero) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * _rotationSpeed
        );
    }

    public void Jump(float jumpPower)
    {
        var vel = Rigidbody.velocity;
        vel.y = jumpPower * 10f;
        Rigidbody.velocity = vel;

        Animator.SetTrigger("doJump");
    }

    public void Dive(Vector3 direction, float force)
    {
        if (_currentDiveCoroutine != null)
        {
            StopCoroutine(_currentDiveCoroutine);
        }

        _isDiving = true;
        Animator.SetTrigger("doDash");

        Rigidbody.useGravity = false;

        Vector3 horizontalDirection = new Vector3(direction.x, 0f, direction.z).normalized;
        Rigidbody.velocity = Vector3.zero;
        Rigidbody.AddForce(horizontalDirection * force * 10f, ForceMode.VelocityChange);

        _targetVelocity = Vector3.zero;

        _currentDiveCoroutine = StartCoroutine(DiveRoutine());

        Debug.Log($"Dive 시작! 방향: {horizontalDirection}, 힘: {force}");
    }

    private IEnumerator DiveRoutine()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            var vel = Rigidbody.velocity;
            vel.x *= 0.95f;
            vel.z *= 0.95f;
            vel.y = 0f;
            Rigidbody.velocity = vel;

            elapsed += Time.fixedDeltaTime;
            yield return _waitForFixedUpdate;
        }

        // Dive 종료 처리
        Rigidbody.useGravity = true;
        _isDiving = false;
        _currentDiveCoroutine = null;

        // Model의 IsDive도 false로 설정
        if (_model != null)
            _model.IsDive = false;

        Debug.Log("Dive 종료! Model.IsDive = false");
    }
    public void StopMovement()
    {
        _targetVelocity = Vector3.zero;
        Rigidbody.velocity = Vector3.zero;
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
}