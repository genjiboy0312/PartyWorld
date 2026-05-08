using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class PlayerPresenter : MonoBehaviour, IPunObservable
{
    [Header("Settings MVP")]
    [SerializeField] private PlayerModel _model;
    [SerializeField] private PlayerView _view;
    [SerializeField] private PhotonView _pv;

    [Header("Settings Camera")]
    [SerializeField] private FollowCamera _followCamera;

    [Header("Settings Controller")]
    [SerializeField] private Controller _playerController;
    [SerializeField] private Controller _cameraController;
    [SerializeField] private Button _btnJump;
    [SerializeField] private Button _btnDive;
    [SerializeField] private Button _btnGrap;

    [Header("Movement Setting")]
    [SerializeField] private Rigidbody _rigidbody3D;
    bool _isGrounded = false;
    private readonly HashSet<Collider> _groundColliders = new HashSet<Collider>();

    private Vector3 _networkPos;
    private Quaternion _networkRot;
    private GameManager _gameManager;
    private Transform _transform;
    private Vector3 _cachedMoveDirection;
    private float _inputH;
    private float _inputV;
    private bool HasLocalControl => _pv == null || !PhotonNetwork.InRoom || _pv.IsMine;

    private const float NETWORK_LERP_SPEED = 15f;

    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        ResolveRequiredReferences();

        if (_btnJump != null) _btnJump.onClick.AddListener(Jump);
        if (_btnDive != null) _btnDive.onClick.AddListener(Dive);
        if (_btnGrap != null) _btnGrap.onClick.AddListener(Grap);

        if (_model != null)
        {
            if (_view != null) _view.SetModel(_model);
            _model.OnJumpStateChanged += OnJumpStateChanged;
            _model.OnDiveStateChanged += OnDiveStateChanged;
            _model.OnGrapStateChanged += OnGrapStateChanged;
        }

        _gameManager = GameManager.Instance;
        if (_gameManager != null)
        {
            _gameManager.OnGameStateChangeEvent += OnGameStateChange;
            OnGameStateChange(_gameManager.CurrentGameState);
        }
    }

    private void ResolveRequiredReferences()
    {
        if (_pv == null) _pv = GetComponent<PhotonView>();
        if (_view == null) _view = GetComponent<PlayerView>();
        if (_rigidbody3D == null)
        {
            _rigidbody3D = (_view != null && _view.Rigidbody != null) ? _view.Rigidbody : GetComponent<Rigidbody>();
        }
    }

    private void OnDisable() => UnsubscribeEvents();
    private void OnDestroy()
    {
        UnsubscribeEvents();
        if (_btnJump != null) _btnJump.onClick.RemoveListener(Jump);
        if (_btnDive != null) _btnDive.onClick.RemoveListener(Dive);
        if (_btnGrap != null) _btnGrap.onClick.RemoveListener(Grap);
    }

    private void UnsubscribeEvents()
    {
        if (_gameManager != null) _gameManager.OnGameStateChangeEvent -= OnGameStateChange;
        if (_model != null)
        {
            _model.OnJumpStateChanged -= OnJumpStateChanged;
            _model.OnDiveStateChanged -= OnDiveStateChanged;
            _model.OnGrapStateChanged -= OnGrapStateChanged;
        }
    }

    private void Update()
    {
        if (HasLocalControl)
        {
            HandleInput();
            HandleCameraInput();
        }
        else
        {
            InterpolateNetworkTransform();
        }
    }

    private void HandleCameraInput()
    {
        if (_followCamera == null) return;

        float h = 0f;
        float v = 0f;

        if (_cameraController != null)
        {
            h += _cameraController.InputHorizontal();
            v += _cameraController.InputVertical();
        }

        float arrowH = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f);
        float arrowV = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);

        if (_cameraController != null)
        {
            if (Mathf.Abs(arrowH) > 0.01f || Mathf.Abs(arrowV) > 0.01f)
                _cameraController.SetJoystickInput(new Vector2(arrowH, arrowV));
            else if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
                _cameraController.ResetJoystick();
        }
        
        h += arrowH;
        v += arrowV;

        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            _followCamera.AddRotation(h, v);
        }
    }

    private void HandleInput()
    {
        float h = 0f;
        float v = 0f;

        // 조이스틱 입력 합산
        if (_playerController != null)
        {
            h += _playerController.InputHorizontal();
            v += _playerController.InputVertical();
        }

        // 키보드 입력
        float wasdH = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
        float wasdV = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);

        // UI 동기화
        if (_playerController != null)
        {
            if (Mathf.Abs(wasdH) > 0.01f || Mathf.Abs(wasdV) > 0.01f)
                _playerController.SetJoystickInput(new Vector2(wasdH, wasdV));
            else if (Mathf.Abs(h) < 0.01f && Mathf.Abs(v) < 0.01f)
                _playerController.ResetJoystick();
        }

        h += wasdH;
        v += wasdV;

        _inputH = Mathf.Clamp(h, -1f, 1f);
        _inputV = Mathf.Clamp(v, -1f, 1f);

        _cachedMoveDirection = new Vector3(_inputH, 0f, _inputV);
        if (_model != null) _model.MoveDirection = _cachedMoveDirection;

        if (Input.GetKeyDown(KeyCode.Space)) Jump();
        if (Input.GetKeyDown(KeyCode.LeftShift)) Dive();
        if (Input.GetKeyDown(KeyCode.LeftControl)) Grap();
    }

    private void FixedUpdate()
    {
        if (HasLocalControl && _rigidbody3D != null && !_rigidbody3D.isKinematic)
        {
            UpdateVelocityMovement();
        }
    }

    private void UpdateVelocityMovement()
    {
        Vector3 moveDir = new Vector3(_inputH, 0, _inputV).normalized;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            _rigidbody3D.MoveRotation(Quaternion.Slerp(_rigidbody3D.rotation, targetRotation, Time.fixedDeltaTime * 15f));
        }

        Vector3 targetVelocity = moveDir * (_model != null ? _model.Speed : 10f);
        Vector3 velocityChange = (targetVelocity - _rigidbody3D.linearVelocity);
        velocityChange.y = 0;
        _rigidbody3D.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void JumpWithVelocity()
    {
        if (_rigidbody3D == null || _rigidbody3D.isKinematic || !_isGrounded) return;

        Vector3 velocity = _rigidbody3D.linearVelocity;
        velocity.y = 0f;
        _rigidbody3D.linearVelocity = velocity;

        float jumpForce = _model != null ? _model.JumpPower : 1f;
        _rigidbody3D.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        _groundColliders.Clear();
        _isGrounded = false;
    }

    private void Jump()
    {
        if (_model != null && _model.CanJump() && _isGrounded)
        {
            _model.IsJump = true;
            if (_view != null) _view.Jump(_model.JumpPower);
        }
    }

    private void Dive() { if (_model != null && _model.CanDive()) _model.IsDive = true; }
    private void Grap() { if (_model != null && _model.CanGrap()) _model.IsGrap = true; }

    private void OnCollisionEnter(Collision collision) => UpdateGroundedState(collision);
    private void OnCollisionStay(Collision collision) => UpdateGroundedState(collision);
    private void OnCollisionExit(Collision collision)
    {
        _groundColliders.Remove(collision.collider);
        _isGrounded = _groundColliders.Count > 0;
    }

    private void UpdateGroundedState(Collision collision)
    {
        if (IsGroundCollision(collision))
        {
            _groundColliders.Add(collision.collider);
            _isGrounded = true;
            if (_model != null) _model.IsJump = false;
        }
    }

    private bool IsGroundCollision(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor")) return true;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f) return true;
        }
        return false;
    }

    private void InterpolateNetworkTransform()
    {
        float deltaTime = Time.deltaTime * NETWORK_LERP_SPEED;
        _transform.position = Vector3.Lerp(_transform.position, _networkPos, deltaTime);
        _transform.rotation = Quaternion.Slerp(_transform.rotation, _networkRot, deltaTime);
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

    private void OnGameStateChange(GameState newGameState) { }
    private void OnJumpStateChanged(bool isJump) { if (isJump) JumpWithVelocity(); }
    private void OnDiveStateChanged(bool isDive) { }
    private void OnGrapStateChanged(bool isGrap) { }
    private void OnSpeedChanged(float newSpeed) { }
}
