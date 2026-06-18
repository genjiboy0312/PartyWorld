using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraStickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Setting JoyStick")]
    [SerializeField] private Image _joyStickBackground;
    [SerializeField] private Image _joyStickImage;

    [Header("Setting Sensitivity")]
    [SerializeField] private float _sensitivity = 0.5f;

    private Vector2 _deltaInput;
    private Vector2 _visualOffset;
    private bool _isPointerDown;
    private bool _isKeyboardActive;

    private void Update()
    {
        float arrowH = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f);
        float arrowV = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) + (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);

        if (Mathf.Abs(arrowH) > 0.01f || Mathf.Abs(arrowV) > 0.01f)
        {
            _isKeyboardActive = true;
            _deltaInput = new Vector2(arrowH, arrowV) * _sensitivity;
            SyncVisualPosition(new Vector2(arrowH, arrowV));
        }
        else if (_isKeyboardActive)
        {
            _isKeyboardActive = false;
            if (!_isPointerDown) ResetCameraStickState();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
        _deltaInput = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _deltaInput = eventData.delta * _sensitivity;

        if (_joyStickBackground == null || _joyStickImage == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _joyStickBackground.rectTransform, eventData.position,
            eventData.pressEventCamera, out Vector2 localPos))
        {
            _visualOffset = new Vector2(
                localPos.x / (_joyStickBackground.rectTransform.sizeDelta.x / 2),
                localPos.y / (_joyStickBackground.rectTransform.sizeDelta.y / 2));
            _visualOffset = Vector2.ClampMagnitude(_visualOffset, 1.0f);
            SyncVisualPosition(_visualOffset);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        if (!_isKeyboardActive) ResetCameraStickState();
    }

    public void SetJoystickInput(Vector2 input)
    {
        SyncVisualPosition(Vector2.ClampMagnitude(input, 1.0f));
    }

    public void ResetJoystick()
    {
        ResetCameraStickState();
    }

    private void SyncVisualPosition(Vector2 normalizedInput)
    {
        if (_joyStickBackground == null || _joyStickImage == null) return;
        _joyStickImage.rectTransform.anchoredPosition = new Vector2(
            normalizedInput.x * (_joyStickBackground.rectTransform.sizeDelta.x / 4),
            normalizedInput.y * (_joyStickBackground.rectTransform.sizeDelta.y / 4));
    }

    private void ResetCameraStickState()
    {
        _deltaInput = Vector2.zero;
        _visualOffset = Vector2.zero;
        if (_joyStickImage != null)
            _joyStickImage.rectTransform.anchoredPosition = Vector2.zero;
        _isPointerDown = false;
    }

    public Vector2 GetDelta()
    {
        return (_isPointerDown || _isKeyboardActive) ? _deltaInput : Vector2.zero;
    }

    public void ResetDelta()
    {
        _deltaInput = Vector2.zero;
    }
}
