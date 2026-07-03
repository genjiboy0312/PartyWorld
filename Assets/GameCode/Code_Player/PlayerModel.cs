using System;
using UnityEngine;

[System.Serializable]
public class PlayerModel
{
    // --- 이벤트 (옵저버 패턴) ---
    public event Action<bool> OnJumpStateChanged;
    public event Action<bool> OnDiveStateChanged;
    public event Action<bool> OnDashStateChanged;
    public event Action<float> OnSpeedChanged;

    [Header("Movement Settings")]
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _jumpPower = 20f;
    [SerializeField] private float _diveForce = 20f;

    [Header("Dash Settings")]
    [SerializeField] private float _dashDelay = 1f;
    private float _dashCooldownTimer;

    [Header("Status (Read Only)")]
    [SerializeField] private bool _isJump;
    [SerializeField] private bool _isDive;
    [SerializeField] private bool _isDash;
    private Vector3 _moveDirection;

    public float Speed
    {
        get => _speed;
        set
        {
            if (Mathf.Approximately(_speed, value)) return;
            _speed = value;
            SafeInvoke(OnSpeedChanged, _speed);
        }
    }

    public float JumpPower => _jumpPower;
    public float DiveForce => _diveForce;
    public float DashDelay => _dashDelay;
    public float DashCooldownRemaining => _dashCooldownTimer;
    public float DashCooldownProgress => _dashDelay > 0f ? 1f - _dashCooldownTimer / _dashDelay : 1f;
    public void SetDashCooldown() => _dashCooldownTimer = _dashDelay;
    public void TickDashCooldown(float delta) => _dashCooldownTimer = Mathf.Max(0f, _dashCooldownTimer - delta);

    public bool IsJump
    {
        get => _isJump;
        set
        {
            if (_isJump == value) return;
            _isJump = value;
            SafeInvoke(OnJumpStateChanged, _isJump);
        }
    }

    public bool IsDive
    {
        get => _isDive;
        set
        {
            if (_isDive == value) return;
            _isDive = value;
            SafeInvoke(OnDiveStateChanged, _isDive);
            Debug.Log($"Model IsDive 설정: {value}"); // 디버그용
        }
    }

    public bool IsDash
    {
        get => _isDash;
        set
        {
            if (_isDash == value) return;
            _isDash = value;
            SafeInvoke(OnDashStateChanged, _isDash);
        }
    }

    public Vector3 MoveDirection
    {
        get => _moveDirection;
        set => _moveDirection = value;
    }

    public bool CanJump() => !_isJump && !_isDash;
    public bool CanDive() => !_isDive;
    public bool CanDash() => !_isJump && !_isDive && !_isDash && _dashCooldownTimer <= 0f;

    public void ResetStates()
    {
        IsJump = false;
        IsDive = false;
        IsDash = false;
        Debug.Log("Model 상태 리셋 완료");
    }

    public bool CanMove() => !_isDive;

    // --- 안전한 이벤트 호출 도우미 ---
    private void SafeInvoke<T>(Action<T> action, T value)
    {
        if (action == null) return;

        foreach (Delegate subscriber in action.GetInvocationList())
        {
            try
            {
                (subscriber as Action<T>)?.Invoke(value);
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerModel 이벤트 호출 오류 [{subscriber.Method.Name}]: {e.Message}");
            }
        }
    }
}