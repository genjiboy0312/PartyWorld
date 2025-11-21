using UnityEngine;

[System.Serializable]
public class PlayerModel
{
    [SerializeField] private float _speed = 30f;
    [SerializeField] private float _jumpPower = 20f;
    [SerializeField] private float _diveForce = 20f;

    [SerializeField] private bool _isJump;
    [SerializeField] private bool _isDive;
    private Vector3 _moveDirection;

    public float Speed
    {
        get => _speed;
        set => _speed = value;
    }

    public float JumpPower => _jumpPower;
    public float DiveForce => _diveForce;

    public bool IsJump
    {
        get => _isJump;
        set => _isJump = value;
    }

    public bool IsDive
    {
        get => _isDive;
        set
        {
            _isDive = value;
            Debug.Log($"Model IsDive 설정: {value}"); // 디버그용
        }
    }

    public Vector3 MoveDirection
    {
        get => _moveDirection;
        set => _moveDirection = value;
    }

    public bool CanJump() => !_isJump && !_isDive;
    public bool CanDive() => !_isJump && !_isDive;

    public void ResetStates()
    {
        _isJump = false;
        _isDive = false;
        Debug.Log("Model 상태 리셋 완료");
    }

    public bool CanMove() => !_isDive;
}