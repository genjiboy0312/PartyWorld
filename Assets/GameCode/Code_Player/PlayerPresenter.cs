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

        // View에 Model 참조 전달
        _view.SetModel(_model);

        // GameManager 구독 ⭐ 중요!
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
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        // 명시적 정리
        UnsubscribeEvents();

        if (_btnJump != null)
            _btnJump.onClick.RemoveListener(Jump);
        if (_btnDive != null)
            _btnDive.onClick.RemoveListener(Dive);
    }

    private void UnsubscribeEvents()
    {
        if (_gameManager != null)
        {
            _gameManager.OnGameStateChangeEvent -= OnGameStateChange;
        }
    }

    private void Update()
    {
        if (!_pv.IsMine)
        {
            InterpolateNetworkTransform();
            return;
        }

        // 게임 Playing 상태일 때만 입력 처리
        if (_gameManager == null || _gameManager.CurrentGameState != GameState.Playing)
            return;

        HandleInput();
    }

    private void FixedUpdate()
    {
        if (!_pv.IsMine || _gameManager == null ||
            _gameManager.CurrentGameState != GameState.Playing)
            return;

        UpdateMovement();
    }

    private void InterpolateNetworkTransform()
    {
        float deltaTime = Time.deltaTime * NETWORK_LERP_SPEED;
        _transform.position = Vector3.Lerp(_transform.position, _networkPos, deltaTime);
        _transform.rotation = Quaternion.Slerp(_transform.rotation, _networkRot, deltaTime);
    }

    private void HandleInput()
    {
        _inputH = _controller ? _controller.InputHorizontal() : Input.GetAxisRaw("Horizontal");
        _inputV = _controller ? _controller.InputVertical() : Input.GetAxisRaw("Vertical");

        _cachedMoveDirection.x = _inputH;
        _cachedMoveDirection.y = 0f;
        _cachedMoveDirection.z = _inputV;

        _model.MoveDirection = _cachedMoveDirection;

        if (Input.GetKeyDown(KeyCode.Space))
            Jump();
        if (Input.GetKeyDown(KeyCode.LeftShift))
            Dive();
    }

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

    private void Jump()
    {
        if (!_model.CanJump())
            return;

        _model.IsJump = true;
        _view.Jump(_model.JumpPower);
    }

    private void Dive()
    {
        if (!_model.CanDive())
            return;

        _model.IsDive = true;

        Vector3 dashDirection = _view.transform.forward;
        _view.Dive(dashDirection, _model.DiveForce);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_model.IsDive)
            return;

        if (collision.gameObject.CompareTag("Floor"))
        {
            _model.ResetStates();
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

    // GameState 변경 시 호출됨 ⭐
    private void OnGameStateChange(GameState newGameState)
    {
        switch (newGameState)
        {
            case GameState.Title:
                // 타이틀 화면
                _model.Speed = 0f;
                _view.StopMovement();
                break;

            case GameState.Loading:
                // 로딩 중
                _model.Speed = 0f;
                _view.StopMovement();
                break;

            case GameState.Playing:
                // 게임 플레이 중
                _model.Speed = 10f;
                // 필요시 애니메이션 활성화 등
                break;

            case GameState.GameOver:
                // 게임 오버
                _model.Speed = 0f;
                _view.StopMovement();

                // 게임 오버 상태 처리
                if (_model.IsDive)
                {
                    _view.ResetDiveState();
                    _model.IsDive = false;
                }
                break;
        }

        Debug.Log($"[{gameObject.name}] GameState 변경: {newGameState}");
    }
}