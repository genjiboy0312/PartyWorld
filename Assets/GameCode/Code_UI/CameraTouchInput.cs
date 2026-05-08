using UnityEngine;
using UnityEngine.EventSystems;

public class CameraTouchInput : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private FollowCamera _followCamera;
    [SerializeField] private float _sensitivity = 0.1f;

    private Vector2 _lastTouchPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        _lastTouchPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // AddRotation이 제거되어 더 이상 사용하지 않음
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // No action needed on pointer up
    }

    private void Start()
    {
        if (_followCamera == null)
        {
            _followCamera = Camera.main.GetComponent<FollowCamera>();
        }
    }
}
