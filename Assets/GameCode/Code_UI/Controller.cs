using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 해당 스크립트는 UI에 직접적으로 넣어서 사용해야함
/// 
/// </summary>
public class Controller : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Setting JoyStick")]
    [SerializeField] private Image _joyStickBackground;
    [SerializeField] private Image _joyStickImage;

    private Vector2 _posInput;
    private bool _isDragging = false;

    [Header("Setting Buttons")]
    [SerializeField] private Button _jumpBtn;
    [SerializeField] private Button _diveBtn;

    void Start()
    {
        _jumpBtn.onClick.AddListener(PlayerJump);
        _diveBtn.onClick.AddListener(PlayerDive);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_joyStickBackground == null || _joyStickImage == null)
        {
            Debug.LogWarning("조이스틱 이미지가 연결X");
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _joyStickBackground.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out _posInput))
        {
            _posInput.x = _posInput.x / (_joyStickBackground.rectTransform.sizeDelta.x / 2);
            _posInput.y = _posInput.y / (_joyStickBackground.rectTransform.sizeDelta.y / 2);

            _posInput = Vector2.ClampMagnitude(_posInput, 1.0f);

            _joyStickImage.rectTransform.anchoredPosition = new Vector2(
                _posInput.x * (_joyStickBackground.rectTransform.sizeDelta.x / 4),
                _posInput.y * (_joyStickBackground.rectTransform.sizeDelta.y / 4)
            );

            _isDragging = true;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
        _isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _posInput = Vector2.zero;
        _joyStickImage.rectTransform.anchoredPosition = Vector2.zero;
        _isDragging = false;
    }

    public float InputHorizontal()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return _isDragging ? _posInput.x : Input.GetAxisRaw("Horizontal");
#else
        return _isDragging ? _posInput.x : 0f;
#endif
    }

    public float InputVertical()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return _isDragging ? _posInput.y : Input.GetAxisRaw("Vertical");
#else
        return _isDragging ? _posInput.y : 0f;
#endif
    }

    private void PlayerJump()
    {
        // Presenter에서 처리할 예정
        Debug.Log("Jump 버튼 눌림");
    }

    private void PlayerDive()
    {
        // Presenter에서 처리할 예정
        Debug.Log("Dive 버튼 눌림");
    }
}
