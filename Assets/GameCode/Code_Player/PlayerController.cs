//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Photon.Pun;
//using UnityEngine.AI;
//using Photon.Realtime;

//public class PlayerController : MonoBehaviour, IPunObservable
//{
//    // 입력 관련
//    private float _horizontalAxis;
//    private float _verticalAxis;

//    [SerializeField] private bool _playerJump;
//    [SerializeField] private bool _playerDive;
//    [SerializeField] private bool _isJump;
//    [SerializeField] private bool _isDive;
//    [SerializeField] private float _jumpPower = 20f;

//    // 컴포넌트
//    private Vector3 _moveVec;
//    private Vector3 _diveVec;
//    private Rigidbody _playerRigidbody;
//    private Animator _animator;
//    private NavMeshAgent _navMeshAgent;
//    private Controller _controller;

//    // Photon
//    private PhotonView _pv;

//    [Header("Player State")]
//    [SerializeField] private float _speed = 10f;

//    private void Awake()
//    {
//        _animator = GetComponentInChildren<Animator>();
//        _playerRigidbody = GetComponent<Rigidbody>();
//        _navMeshAgent = GetComponent<NavMeshAgent>();
//        _pv = GetComponent<PhotonView>();
//    }

//    private void Start()
//    {
//        _joyStick = GameObject.Find("BackGround_JoyStick")?.GetComponent<_controller>();

//        // 옵저버 패턴 등록
//        if (GameManager.Instance != null)
//            GameManager.Instance.OnGameStateChangeEvent += OnGameStateChange;
//    }

//    private void OnDisable()
//    {
//        // 옵저버 해제 (PhotonNetwork.Destroy 등에서도 안전)
//        if (GameManager.Instance != null)
//            GameManager.Instance.OnGameStateChangeEvent -= OnGameStateChange;
//    }

//    private void Update()
//    {
//        if (!_pv.IsMine || GameManager.Instance.CurrentGameState != GameState.Playing)
//            return;

//        HandleInput();
//        PlayerMove();
//        PlayerTurn();
//        PlayerJump();
//        PlayerDash();
//    }

//    // 입력 처리
//    private void HandleInput()
//    {
//        _horizontalAxis = Input.GetAxisRaw("Horizontal");
//        _verticalAxis = Input.GetAxisRaw("Vertical");

//        if (_joyStick != null)
//        {
//            _horizontalAxis = _joyStick.inputHorizontal();
//            _verticalAxis = _joyStick.inputVertical();
//        }

//        _playerJump = Input.GetKeyDown(KeyCode.Space);
//        _playerDive = Input.GetKeyDown(KeyCode.LeftShift);
//    }

//    #region Player Movement

//    private void PlayerMove()
//    {
//        _moveVec = new Vector3(_horizontalAxis, 0, _verticalAxis).normalized;
//        if (_isDive)
//            _moveVec = _diveVec;

//        Vector3 targetVelocity = _moveVec * _speed;
//        targetVelocity.y = _playerRigidbody.velocity.y;
//        _playerRigidbody.velocity = targetVelocity;

//        _animator.SetBool("isMove", _moveVec != Vector3.zero);
//    }

//    private void PlayerTurn()
//    {
//        if (_moveVec != Vector3.zero)
//            transform.LookAt(transform.position + _moveVec);
//    }

//    private void PlayerJump()
//    {
//        if (_playerJump && !_isJump && !_isDive)
//        {
//            _navMeshAgent.enabled = false;

//            Vector3 newVelocity = _playerRigidbody.velocity;
//            newVelocity.y = _jumpPower;
//            _playerRigidbody.velocity = newVelocity;

//            _animator.SetTrigger("doJump");
//            _isJump = true;
//        }
//    }

//    private void PlayerDash()
//    {
//        if (_playerDive && !_isJump && _moveVec != Vector3.zero && !_isDive)
//        {
//            _diveVec = _moveVec;
//            _speed *= 2;
//            _animator.SetTrigger("doDash");
//            _isDive = true;

//            Invoke(nameof(EndDash), 0.2f);
//        }
//    }

//    private void EndDash()
//    {
//        _speed *= 0.5f;
//        _isDive = false;
//    }

//    #endregion

//    private void OnCollisionEnter(Collision collision)
//    {
//        if (collision.gameObject.CompareTag("Floor"))
//        {
//            _isJump = false;
//            _isDive = false;
//            _navMeshAgent.enabled = true;
//        }
//    }

//    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
//    {
//        if (stream.IsWriting)
//        {
//            stream.SendNext(transform.position);
//            stream.SendNext(transform.rotation);
//        }
//        else
//        {
//            transform.position = (Vector3)stream.ReceiveNext();
//            transform.rotation = (Quaternion)stream.ReceiveNext();
//        }
//    }

//    // 옵저버 이벤트: GameManager 상태 변경 처리
//    private void OnGameStateChange(GameState newGameState)
//    {
//        switch (newGameState)
//        {
//            case GameState.Playing:
//                _speed = 10f;
//                _navMeshAgent.enabled = true;
//                break;
//            case GameState.GameOver:
//                _speed = 0f;
//                _navMeshAgent.enabled = false;
//                _playerRigidbody.velocity = Vector3.zero;
//                break;
//        }
//    }
//}