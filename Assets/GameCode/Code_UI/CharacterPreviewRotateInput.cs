using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterPreviewRotateInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerClickHandler
{
    [SerializeField] private Transform _targetRoot;
    [SerializeField] private float _dragSensitivity = 0.25f;
    [SerializeField] private float _clickStepDegrees = 12f;
    [SerializeField] private bool _invertDrag = false;
    [SerializeField] private float _dragClickCancelThreshold = 6f;

    private RectTransform _rectTransform;
    private Vector2 _pointerDownPos;
    private bool _dragMoved;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;

        if (_targetRoot == null)
            Debug.LogWarning("[CharacterPreviewRotateInput] _targetRoot is not assigned.", this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDownPos = eventData.position;
        _dragMoved = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_targetRoot == null)
            return;

        if (!_dragMoved && Vector2.Distance(_pointerDownPos, eventData.position) >= _dragClickCancelThreshold)
            _dragMoved = true;

        float direction = _invertDrag ? -1f : 1f;
        float yaw = eventData.delta.x * _dragSensitivity * direction;
        _targetRoot.Rotate(0f, -yaw, 0f, Space.World);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_targetRoot == null || _rectTransform == null)
            return;

        if (_dragMoved)
            return;

        bool isRightSide = IsRightSideClick(eventData);
        float sign = isRightSide ? -1f : 1f;
        _targetRoot.Rotate(0f, sign * _clickStepDegrees, 0f, Space.World);
    }

    private bool IsRightSideClick(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        return localPoint.x >= 0f;
    }
}
