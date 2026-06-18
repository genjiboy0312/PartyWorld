using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JoyStickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Setting JoyStick")]
    [SerializeField] private Image _joyStickBackground;
    [SerializeField] private Image _joyStickImage;

    private Vector2 _posInput;
    private bool _isPointerDown;
    private bool _isKeyboardActive;

    private void Update()
    {
        float kH = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
        float kV = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);

        if (Mathf.Abs(kH) > 0.01f || Mathf.Abs(kV) > 0.01f)
        {
            _isKeyboardActive = true;
            _posInput = Vector2.ClampMagnitude(new Vector2(kH, kV), 1f);
            SyncVisualPosition(_posInput);
        }
        else if (_isKeyboardActive)
        {
            _isKeyboardActive = false;
            if (!_isPointerDown) ResetJoyStickState();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_joyStickBackground == null || _joyStickImage == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _joyStickBackground.rectTransform, eventData.position,
            eventData.pressEventCamera, out _posInput))
        {
            _posInput.x = _posInput.x / (_joyStickBackground.rectTransform.sizeDelta.x / 2);
            _posInput.y = _posInput.y / (_joyStickBackground.rectTransform.sizeDelta.y / 2);
            _posInput = Vector2.ClampMagnitude(_posInput, 1.0f);

            _joyStickImage.rectTransform.anchoredPosition = new Vector2(
                _posInput.x * (_joyStickBackground.rectTransform.sizeDelta.x / 4),
                _posInput.y * (_joyStickBackground.rectTransform.sizeDelta.y / 4));

            _isPointerDown = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
        _isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        if (!_isKeyboardActive) ResetJoyStickState();
    }

    public void SetJoystickInput(Vector2 input)
    {
        SyncVisualPosition(Vector2.ClampMagnitude(input, 1.0f));
    }

    public void ResetJoystick()
    {
        ResetJoyStickState();
    }

    private void SyncVisualPosition(Vector2 normalizedInput)
    {
        if (_joyStickBackground == null || _joyStickImage == null) return;
        _joyStickImage.rectTransform.anchoredPosition = new Vector2(
            normalizedInput.x * (_joyStickBackground.rectTransform.sizeDelta.x / 4),
            normalizedInput.y * (_joyStickBackground.rectTransform.sizeDelta.y / 4));
    }

    private void ResetJoyStickState()
    {
        _posInput = Vector2.zero;
        if (_joyStickImage != null)
            _joyStickImage.rectTransform.anchoredPosition = Vector2.zero;
        _isPointerDown = false;
    }

    public float InputHorizontal() => (_isPointerDown || _isKeyboardActive) ? _posInput.x : 0f;
    public float InputVertical() => (_isPointerDown || _isKeyboardActive) ? _posInput.y : 0f;
}
