using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.AI;
using Photon.Realtime;

public class PlayerController : MonoBehaviour, IPunObservable
{
    // 입력 관련
    private float _horizontalAxis;
    private float _verticalAxis;

    [SerializeField] private bool _playerJump;
    [SerializeField] private bool _playerDash;
    [SerializeField] private bool _isJump;
    [SerializeField] private bool _isDash;
    [SerializeField] private float _jumpPower = 20f;

    // 컴포넌트
    private Vector3 _moveVec;
    private Vector3 _dashVec;
    private Rigidbody _playerRigidbody;
    private Animator _animator;
    private NavMeshAgent _navMeshAgent;
    private JoyStick _joyStick;

    // Photon
    public PhotonView _pv;

    [Header("Player State")]
    [SerializeField] private float _speed = 10f;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _playerRigidbody = GetComponent<Rigidbody>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        _joyStick = GameObject.Find("BackGround_JoyStick")?.GetComponent<JoyStick>();

        // GameManager 상태 변경 이벤트 구독
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChangeEvent += OnGameStateChange;
    }

    private void Update()
    {
        if (!_pv.IsMine || GameManager.Instance.CurrentGameState != GameState.Playing)
            return;

        HandleInput();
        PlayerMove();
        PlayerTurn();
        PlayerJump();
        PlayerDash();
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChangeEvent -= OnGameStateChange;
    }

    // 입력 처리
    private void HandleInput()
    {
        _horizontalAxis = Input.GetAxisRaw("Horizontal");
        _verticalAxis = Input.GetAxisRaw("Vertical");

        if (_joyStick != null)
        {
            _horizontalAxis = _joyStick.inputHorizontal();
            _verticalAxis = _joyStick.inputVertical();
        }

        _playerJump = Input.GetKeyDown(KeyCode.Space);
        _playerDash = Input.GetKeyDown(KeyCode.LeftShift);
    }

    #region Player Movement

    private void PlayerMove()
    {
        _moveVec = new Vector3(_horizontalAxis, 0, _verticalAxis).normalized;
        if (_isDash)
            _moveVec = _dashVec;

        Vector3 targetVelocity = _moveVec * _speed;
        targetVelocity.y = _playerRigidbody.velocity.y;
        _playerRigidbody.velocity = targetVelocity;

        _animator.SetBool("isMove", _moveVec != Vector3.zero);
    }

    private void PlayerTurn()
    {
        if (_moveVec != Vector3.zero)
            transform.LookAt(transform.position + _moveVec);
    }

    private void PlayerJump()
    {
        if (_playerJump && !_isJump && !_isDash)
        {
            _navMeshAgent.enabled = false;

            Vector3 newVelocity = _playerRigidbody.velocity;
            newVelocity.y = _jumpPower;
            _playerRigidbody.velocity = newVelocity;

            _animator.SetTrigger("doJump");
            _isJump = true;
        }
    }

    private void PlayerDash()
    {
        if (_playerDash && !_isJump && _moveVec != Vector3.zero && !_isDash)
        {
            _dashVec = _moveVec;
            _speed *= 2;
            _animator.SetTrigger("doDash");
            _isDash = true;

            Invoke(nameof(EndDash), 0.2f);
        }
    }

    private void EndDash()
    {
        _speed *= 0.5f;
        _isDash = false;
    }

    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            _isJump = false;
            _isDash = false;
            _navMeshAgent.enabled = true;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
        }
    }

    // GameManager 상태 이벤트 처리
    private void OnGameStateChange(GameState newGameState)
    {
        switch (newGameState)
        {
            case GameState.Playing:
                _speed = 10f;
                break;
            case GameState.GameOver:
                _speed = 0f;
                break;
        }
    }
}
