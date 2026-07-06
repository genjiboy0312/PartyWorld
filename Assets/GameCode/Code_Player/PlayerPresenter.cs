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
    private IPlayerInputProvider _playerInput;
    private ICameraInputProvider _cameraInput;

    public void BindInputProviders(IPlayerInputProvider playerInput, ICameraInputProvider cameraInput)
    {
        _playerInput = playerInput;
        _cameraInput = cameraInput;
    }
    [SerializeField] private Button _btnJump;
    [SerializeField] private Button _btnDive;
    [SerializeField] private Button _btnDash;       //  Dash Attack
    [SerializeField] private Image _dashCooldownImage;  // Dash 재사용 대기시간 오버레이
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
        if (_btnDash != null) _btnDash.onClick.AddListener(Dash);

        if (_model != null)
        {
            if (_view != null) _view.SetModel(_model);
            _model.OnJumpStateChanged += OnJumpStateChanged;
            _model.OnDiveStateChanged += OnDiveStateChanged;
            _model.OnDashStateChanged += OnDashStateChanged;
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
        if (_btnDash != null) _btnDash.onClick.RemoveListener(Dash);
    }

    private void UnsubscribeEvents()
    {
        if (_gameManager != null) _gameManager.OnGameStateChangeEvent -= OnGameStateChange;
        if (_model != null)
        {
            _model.OnJumpStateChanged -= OnJumpStateChanged;
            _model.OnDiveStateChanged -= OnDiveStateChanged;
            _model.OnDashStateChanged -= OnDashStateChanged;
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

        // 대시 쿨다운 틱 (로컬/원격 모두 UI 업데이트 필요)
        if (_model != null)
        {
            _model.TickDashCooldown(Time.deltaTime);
            UpdateDashCooldownUI();
        }
    }

    private void UpdateDashCooldownUI()
    {
        bool onCooldown = _model != null && !_model.CanDash();

        if (_dashCooldownImage != null)
            _dashCooldownImage.fillAmount = onCooldown ? _model.DashCooldownProgress : 0f;

        if (_btnDash != null)
        {
            var rawImage = _btnDash.GetComponent<UnityEngine.UI.RawImage>();
            if (rawImage != null)
                rawImage.color = onCooldown ? new Color(0.3f, 0.3f, 0.3f, 1f) : Color.white;
            _btnDash.interactable = !onCooldown;
        }
    }
    private void HandleCameraInput()
    {
        if (_followCamera == null) return;

        float h = 0f;
        float v = 0f;

        if (_cameraInput != null)
        {
            Vector2 delta = _cameraInput.GetCameraDelta();
            h += delta.x;
            v += delta.y;
        }

        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            _followCamera.AddRotation(h, v);
        }
        else if (_cameraInput != null)
        {
            _cameraInput.ResetCameraDelta();
        }
    }

    private void HandleInput()
    {
        float h = 0f;
        float v = 0f;

        if (_playerInput != null)
        {
            Vector2 move = _playerInput.GetMoveInput();
            h += move.x;
            v += move.y;
        }

        _inputH = Mathf.Clamp(h, -1f, 1f);
        _inputV = Mathf.Clamp(v, -1f, 1f);

        _cachedMoveDirection = new Vector3(_inputH, 0f, _inputV);
        if (_model != null) _model.MoveDirection = _cachedMoveDirection;

        if (Input.GetKeyDown(KeyCode.Space)) Jump();
        if (Input.GetKeyDown(KeyCode.LeftShift)) Dive();
        if (Input.GetKeyDown(KeyCode.LeftControl)) Dash();
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
        Vector3 inputDir = new Vector3(_inputH, 0, _inputV);
        if (inputDir.sqrMagnitude <= 0.01f)
        {
            if (_rigidbody3D != null)
            {
                Vector3 zeroVelocity = _rigidbody3D.linearVelocity;
                zeroVelocity.x = 0f;
                zeroVelocity.z = 0f;
                _rigidbody3D.linearVelocity = zeroVelocity;
            }
            return;
        }

        Transform cameraTransform = _followCamera != null ? _followCamera.transform : Camera.main.transform;
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        Vector3 moveDir = (camForward * inputDir.z + camRight * inputDir.x).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
        _rigidbody3D.MoveRotation(Quaternion.Slerp(_rigidbody3D.rotation, targetRotation, Time.fixedDeltaTime * 15f));

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

    [Header("Dash Setting")]
    [SerializeField] private float _dashForce = 20f;
    private bool _isDashing = false;

    // 대시
    private void Dash() 
    { 
        if (_model != null && _model.CanDash()) 
        {
            if (_view != null) _view.DashAnimation();
            _model.IsDash = true;
            _model.SetDashCooldown();
            StartCoroutine(DashCoroutine());
        }
    }

    private System.Collections.IEnumerator DashCoroutine()
    {
        _isDashing = true;
        Vector3 dashDir = transform.forward;
        float _holdTime = 0.35f;
        float _totalTime = 0.65f;
        float _elapsed = 0f;

        // 초기 AddForce 점진 가속
        _rigidbody3D.linearVelocity = Vector3.zero;
        _rigidbody3D.AddForce(dashDir * _dashForce * 5f, ForceMode.Acceleration);

        while (_elapsed < _totalTime)
        {
            if (_elapsed < _holdTime)
            {
                // AddForce 지속 가속
                _rigidbody3D.AddForce(dashDir * 100f, ForceMode.Acceleration);
            }
            else
            {
                // 저항력으로 부드러운 감속
                Vector3 planarVel = new Vector3(_rigidbody3D.linearVelocity.x, 0f, _rigidbody3D.linearVelocity.z);
                _rigidbody3D.AddForce(-planarVel * 2f, ForceMode.Acceleration);
            }
            // Y축 속도 고정
            _rigidbody3D.linearVelocity = new Vector3(_rigidbody3D.linearVelocity.x, 0f, _rigidbody3D.linearVelocity.z);

            _elapsed += Time.deltaTime;
            yield return null;
        }

        _isDashing = false;
        if (_model != null) _model.IsDash = false;
    }

    // 다이브
    private void Dive() 
    { 
        if (_model != null && _model.CanDive()) 
        {
            _model.IsDive = true;
            if (_view != null) _view.Dive(transform.forward, _model.DiveForce);
        }
    }

    // 다이브 종료 (버튼 뗄 때 호출)
    public void DiveEnd()
    {
        if (_model != null)
        {
            _model.IsDive = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        bool wasGrounded = _isGrounded;
        UpdateGroundedState(collision);

        // 착지 감지: 공중에서 땅에 닿으면 점프/대시 상태 리셋
        if (!wasGrounded && _isGrounded && _model != null)
        {
            _model.IsJump = false;
            _model.IsDash = false;
        }

        // 대시 중 충돌 시 상대방 밀쳐내기
        if (_isDashing && collision.gameObject.CompareTag("Player"))
        {
            Rigidbody otherRb = collision.gameObject.GetComponent<Rigidbody>();
            if (otherRb != null)
            {
                Vector3 pushDir = (collision.transform.position - transform.position).normalized;
                otherRb.AddForce(pushDir * (_dashForce * 0.5f), ForceMode.Impulse);
            }
        }
    }
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
    private void OnSpeedChanged(float newSpeed) { }
    private void OnDashStateChanged(bool isDash) { }
}
