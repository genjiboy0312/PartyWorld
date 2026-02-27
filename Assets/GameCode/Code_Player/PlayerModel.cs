using System;
using UnityEngine;

[System.Serializable]
public class PlayerModel
{
    // --- 이벤트 (옵저버 패턴) ---
    public event Action<bool> OnJumpStateChanged;
    public event Action<bool> OnDiveStateChanged;
    public event Action<bool> OnGrapStateChanged;
    public event Action<float> OnSpeedChanged;

    [Header("Movement Settings")]
    [SerializeField] private float _speed = 30f;
    [SerializeField] private float _jumpPower = 20f;
    [SerializeField] private float _diveForce = 20f;

    [Header("Status (Read Only)")]
    [SerializeField] private bool _isJump;
    [SerializeField] private bool _isDive;
    [SerializeField] private bool _isGrap;
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

    public bool IsGrap
    {
        get => _isGrap;
        set
        {
            if (_isGrap == value) return;
            _isGrap = value;
            SafeInvoke(OnGrapStateChanged, _isGrap);
        }
    }

    public Vector3 MoveDirection
    {
        get => _moveDirection;
        set => _moveDirection = value;
    }

    public bool CanJump() => !_isJump && !_isDive;
    public bool CanDive() => !_isJump && !_isDive;
    public bool CanGrap() => !_isGrap;

    public void ResetStates()
    {
        IsJump = false;
        IsDive = false;
        IsGrap = false;
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