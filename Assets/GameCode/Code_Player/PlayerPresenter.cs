using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerPresenter : MonoBehaviour, IPunObservable
{
    [Header("Settings MVP")]
    [SerializeField] private PlayerModel _model;
    [SerializeField] private PlayerView _view;
    [SerializeField] private PhotonView _pv;

    [Header("Settings Controller")]
    [SerializeField] private Controller _controller;
    [SerializeField] private Button _btnJump;
    [SerializeField] private Button _btnDive;
    [SerializeField] private Button _btnGrap;

    [Header("JointMove Setting")]
    [SerializeField] private Rigidbody _rigidbody3D;
    [SerializeField] private ConfigurableJoint _mainJoint;
    [SerializeField] private RaycastHit[] _raycastHits = new RaycastHit[10];
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _groundCheckDistance = 0.5f;
    [SerializeField] private float _groundCheckRadius = 0.1f;
    [SerializeField] private float _groundStickForce = 10f;
    bool _isGrounded = false;

    private Vector3 _networkPos;
    private Quaternion _networkRot;
    private GameManager _gameManager;
    private Transform _transform;
    private Vector3 _cachedMoveDirection;
    private float _inputH;
    private float _inputV;

    private const float NETWORK_LERP_SPEED = 15f;

    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        // UI 버튼 이벤트
        if (_btnJump != null)
            _btnJump.onClick.AddListener(Jump);
        if (_btnDive != null)
            _btnDive.onClick.AddListener(Dive);
        if (_btnGrap != null)
            _btnGrap.onClick.AddListener(Grap);

        // PlayerModel 이벤트 구독 (옵저버 패턴)
        if (_model != null)
        {
            _model.OnJumpStateChanged += OnJumpStateChanged;
            _model.OnDiveStateChanged += OnDiveStateChanged;
            _model.OnGrapStateChanged += OnGrapStateChanged;
            _model.OnSpeedChanged += OnSpeedChanged;
        }

        // GameManager 구독 
        _gameManager = GameManager.Instance;
        if (_gameManager != null)
        {
            _gameManager.OnGameStateChangeEvent += OnGameStateChange;

            // 현재 게임 상태에 맞게 초기화
            OnGameStateChange(_gameManager.CurrentGameState);
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] GameManager.Instance가 null입니다!");
        }

        // ConfigurableJoint 초기 설정
        if (_mainJoint != null)
        {
            _mainJoint.xMotion = ConfigurableJointMotion.Free;
            _mainJoint.yMotion = ConfigurableJointMotion.Free;
            _mainJoint.zMotion = ConfigurableJointMotion.Free;
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        // 명시적 정리
        UnsubscribeEvents();

        //  버튼 이벤트 등록
        if (_btnJump != null)
            _btnJump.onClick.RemoveListener(Jump);
        if (_btnDive != null)
            _btnDive.onClick.RemoveListener(Dive);
        if (_btnGrap != null)
            _btnGrap.onClick.RemoveListener(Grap);
    }

    //  이벤트 구독 취소
    private void UnsubscribeEvents()
    {
        if (_gameManager != null)
        {
            _gameManager.OnGameStateChangeEvent -= OnGameStateChange;
        }

        if (_model != null)
        {
            _model.OnJumpStateChanged -= OnJumpStateChanged;
            _model.OnDiveStateChanged -= OnDiveStateChanged;
            _model.OnGrapStateChanged -= OnGrapStateChanged;
            _model.OnSpeedChanged -= OnSpeedChanged;
        }
    }

    // --- PlayerModel 이벤트 핸들러 ---

    private void OnJumpStateChanged(bool isJump)
    {
        if (isJump)
        {
            Debug.Log("[Presenter] Jump 상태 시작");
            JumpWithJoint();
            // _view.PlayJumpAnimation(); // 필요시 구현
        }
    }

    private void OnDiveStateChanged(bool isDive)
    {
        Debug.Log($"[Presenter] Dive 상태 변경: {isDive}");
        if (isDive)
        {
            // 다이빙 물리 효과 등
            // Vector3 dashDirection = transform.forward;
            // _view.Dive(dashDirection, _model.DiveForce);
        }
    }

    private void OnGrapStateChanged(bool isGrap)
    {
        Debug.Log($"[Presenter] Grap 상태 변경: {isGrap}");
        // 잡기 관련 애니메이션 또는 물리 로직 처리
    }

    private void OnSpeedChanged(float newSpeed)
    {
        _moveSpeed = newSpeed;
    }

    private void Update()
    {
        if (!_pv.IsMine)
        {
            InterpolateNetworkTransform();
            return;
        }

        //// 게임 Playing 상태일 때만 입력 처리
        //if (_gameManager == null || _gameManager.CurrentGameState != GameState.Playing)
        //    return;

        HandleInput();
    }

    private void FixedUpdate()
    {
        if (!_pv.IsMine)
            return;

        if (_rigidbody3D == null || _rigidbody3D.isKinematic)
            return;

        // ConfigurableJoint 이용한 움직임 처리
        CheckGroundStatus();
        UpdateJointMovement();

        // 기존 움직임 코드 주석 처리
        // UpdateMovement();
    }

    private void InterpolateNetworkTransform()
    {
        float deltaTime = Time.deltaTime * NETWORK_LERP_SPEED;
        _transform.position = Vector3.Lerp(_transform.position, _networkPos, deltaTime);
        _transform.rotation = Quaternion.Slerp(_transform.rotation, _networkRot, deltaTime);
    }

    //  Input
    private void HandleInput()
    {
        _inputH = _controller ? _controller.InputHorizontal() : Input.GetAxisRaw("Horizontal");
        _inputV = _controller ? _controller.InputVertical() : Input.GetAxisRaw("Vertical");

        _cachedMoveDirection.x = _inputH;
        _cachedMoveDirection.y = 0f;
        _cachedMoveDirection.z = _inputV;

        _model.MoveDirection = _cachedMoveDirection;

        if (Input.GetKeyDown(KeyCode.Space))
            JumpWithJoint();
        if (Input.GetKeyDown(KeyCode.LeftShift))
            Dive();
        if (Input.GetKeyDown(KeyCode.LeftControl))
            Grap();
    }

    // ConfigurableJoint를 이용한 바닥 체크
    private void CheckGroundStatus()
    {
        if (_rigidbody3D == null || _rigidbody3D.isKinematic)
            return;

        _isGrounded = false;

        int numberOfHits = Physics.SphereCastNonAlloc(
            _rigidbody3D.position,
            _groundCheckRadius,
            _transform.up * -1,
            _raycastHits,
            _groundCheckDistance
        );

        // 결과 값을 체크
        for (int i = 0; i < numberOfHits; i++)
        {
            // 자기 자신 무시
            if (_raycastHits[i].transform.root == _transform.root)
                continue;

            _isGrounded = true;
            break;
        }

        // 바닥에 붙어있게 하는 힘
        if (_isGrounded)
        {
            _rigidbody3D.AddForce(Vector3.down * _groundStickForce, ForceMode.Force);
        }
    }

    // ConfigurableJoint를 이용한 이동 처리
    private void UpdateJointMovement()
    {
        if (_rigidbody3D == null || _rigidbody3D.isKinematic)
            return;

        // if (_model.IsDive)
        //     return;

        Vector3 moveDir = _cachedMoveDirection;
        float magnitude = moveDir.sqrMagnitude;

        if (magnitude > 1f)
            moveDir.Normalize();

        // 이동 방향으로 회전
        if (magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            _rigidbody3D.MoveRotation(Quaternion.Slerp(_rigidbody3D.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }

        // 이동 힘 적용
        Vector3 moveForce = moveDir * _moveSpeed;

        // Y축 속도는 유지 (점프/낙하 방해하지 않음)
        Vector3 currentVelocity = _rigidbody3D.linearVelocity;
        Vector3 targetVelocity = new Vector3(moveForce.x, currentVelocity.y, moveForce.z);

        // 부드러운 이동을 위해 velocity 직접 조정
        _rigidbody3D.linearVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
    }

    // ConfigurableJoint용 점프
    private void JumpWithJoint()
    {
        if (_rigidbody3D == null || _rigidbody3D.isKinematic)
            return;

        if (!_isGrounded)
            return;

        // 위쪽으로 힘을 가함
        _rigidbody3D.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
    }

    // 기존 함수들 주석 처리
    /*
    private void UpdateMovement()
    {
        if (_model.IsDive)
            return;

        Vector3 moveDir = _model.MoveDirection;
        float magnitude = moveDir.sqrMagnitude;

        if (magnitude > 1f)
            moveDir.Normalize();

        if (magnitude > 0.01f)
            _view.LookAt(moveDir);

        Vector3 vel = moveDir * _model.Speed;
        vel.y = _view.Rigidbody.velocity.y;
        _view.Move(vel);
    }
    */

    //  점프
    private void Jump()
    {
        if (!_model.CanJump())
            return;

        _model.IsJump = true;
        // _view.Jump(_model.JumpPower);
    }

    //  다이빙
    private void Dive()
    {
        if (!_model.CanDive())
            return;

        _model.IsDive = true;

        // Vector3 dashDirection = _view.transform.forward;
        // _view.Dive(dashDirection, _model.DiveForce);
    }

    //  잡기
    private void Grap()
    {
        if (!_model.CanGrap())
            return;

        _model.IsGrap = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // if (_model.IsDive)
        //     return;

        if (collision.gameObject.CompareTag("Floor"))
        {
            // _model.ResetStates();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_transform.position);
            stream.SendNext(_transform.rotation);
        }
        else
        {
            _networkPos = (Vector3)stream.ReceiveNext();
            _networkRot = (Quaternion)stream.ReceiveNext();
        }
    }

    // GameState 변경 시 호출됨
    private void OnGameStateChange(GameState newGameState)
    {
        switch (newGameState)
        {
            case GameState.Title:
                // 타이틀 화면
                // _model.Speed = 0f;
                // _view.StopMovement();
                break;

            case GameState.Loading:
                // 로딩 중
                // _model.Speed = 0f;
                // _view.StopMovement();
                break;

            case GameState.Playing:
                // 게임 플레이 중
                // _model.Speed = 10f;
                // 필요시 애니메이션 활성화 등
                break;

            case GameState.GameOver:
                // 게임 오버
                // _model.Speed = 0f;
                // _view.StopMovement();

                // 게임 오버 상태 처리
                // if (_model.IsDive)
                // {
                //     _view.ResetDiveState();
                //     _model.IsDive = false;
                // }
                break;
        }

        Debug.Log($"[{gameObject.name}] GameState 변경: {newGameState}");
    }
}
