using UnityEngine;
using Photon.Pun;

public class PlayerPresenter : MonoBehaviour, IPunObservable
{
    [SerializeField] private PlayerModel _model;
    [SerializeField] private PlayerView _view;
    private PhotonView _pv;
    private Controller _controller;

    private void Awake()
    {
        _pv = GetComponent<PhotonView>();
        _controller = GameObject.Find("BackGround_JoyStick")?.GetComponent<Controller>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChangeEvent += OnGameStateChange;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChangeEvent -= OnGameStateChange;
    }

    private void Update()
    {
        if (!_pv.IsMine || GameManager.Instance.CurrentGameState != GameState.Playing)
            return;

        HandleInput(); // 입력만 여기서
    }

    private void FixedUpdate()
    {
        if (!_pv.IsMine || GameManager.Instance.CurrentGameState != GameState.Playing)
            return;

        UpdateMovement(); // 이동은 FixedUpdate에서
    }

    private void HandleInput()
    {
        float _h = _controller != null ? _controller.InputHorizontal() : Input.GetAxisRaw("Horizontal");
        float _v = _controller != null ? _controller.InputVertical() : Input.GetAxisRaw("Vertical");

        // normalize 제거
        _model.MoveDirection = new Vector3(_h, 0, _v);

        if (Input.GetKeyDown(KeyCode.Space))
            Jump();

        if (Input.GetKeyDown(KeyCode.LeftShift))
            Dive();
    }

    private void UpdateMovement()
    {
        Vector3 moveVec = Vector3.ClampMagnitude(_model.MoveDirection, 1f);

        if (_model.IsDive)
            moveVec *= 2f;

        Vector3 targetVelocity = moveVec * _model.Speed;
        targetVelocity.y = _view.Rigidbody.velocity.y;

        _view.Move(targetVelocity);
        _view.LookAt(moveVec);
    }

    private void Jump()
    {
        if (_model.IsJump || _model.IsDive) return;

        _view.Jump(_model.JumpPower);
        _model.IsJump = true;
    }

    private void Dive()
    {
        if (_model.IsDive || _model.IsJump || _model.MoveDirection == Vector3.zero) return;

        _model.IsDive = true;
        _view.Dash();

        Invoke(nameof(EndDive), 0.25f);
    }

    private void EndDive() => _model.IsDive = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
            _model.ResetStates();
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

    private void OnGameStateChange(GameState newGameState)
    {
        switch (newGameState)
        {
            case GameState.Playing:
                _model.Speed = 10f;
                break;
            case GameState.GameOver:
                _model.Speed = 0f;
                _view.StopMovement();
                break;
        }
    }
}
