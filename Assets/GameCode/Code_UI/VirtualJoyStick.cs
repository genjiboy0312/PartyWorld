using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoyStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private Image imageBackground;
    private Image imageController;
    private Vector2 touchPosition;

    void Awake()
    {
        imageBackground = GetComponent<Image>();
        imageController = transform.GetChild(0).GetComponent<Image>();      // backGround 밑 자식으로 있으므로

    }

    // 터치 시작 시 1회
    public void OnPointerDown(PointerEventData eventData)
    {
        //Debug.Log("Touch Begin" + eventData);
    }

    // 터치 상태일 때 매 프레임
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(imageBackground.rectTransform, eventData.position,
            eventData.pressEventCamera, out pos))
        {
            // touchPosition 값의 정규화 [0 ~ 1] , 이미지 크기로 나눔
            pos.x = (pos.x / imageBackground.rectTransform.sizeDelta.x);
            pos.y = (pos.y / imageBackground.rectTransform.sizeDelta.y);

            // touchPosition 값의 정규화 [~n ~ n] , 이미지 크기로 나눔
            // 왼쪽 (-1), 중심 (0), 오른쪽 (1) 로 변경하기위해 touchPosition.x * 2 -1
            // 아래 (-1), 중심 (0), 위(1)로 변경하기 위해 touchPosition.y * 2 -1
            // 이 수식은 Piviot에 따라 달라짐 (좌 하단 Pivot 기준)
            pos = new Vector2(pos.x * 2 - 1, pos.y * 2 - 1);

            // touchPosition 값의 정규화 [-1 ~ 1]
            // 가상 조이스틱 배경 이미지 밖으로 터치가 나가게 되면 -1 ~ 1 보다 큰 값이 나올 수 있음
            // 이때 normailzed를 이용해 -1 ~ 1 사이의 값으로 정규화
            touchPosition = (pos.magnitude > 1) ? pos.normalized : pos;

            // 조이스틱 컨트롤러 이미지 이동
            imageController.rectTransform.anchoredPosition = new Vector2(
                touchPosition.x * imageBackground.rectTransform.sizeDelta.x / 2,
                touchPosition.y * imageBackground.rectTransform.sizeDelta.y / 2);
        }
    }

    // 터치 종료시 1회
    public void OnPointerUp(PointerEventData eventData)
    {
        // 위치 초기화
        imageController.rectTransform.anchoredPosition = Vector2.zero;

        // 다른 오브젝트에서 이동 방향으로 사용하기 때문에 이동 방향도 초기화
        touchPosition = Vector2.zero;
        //Debug.Log("Touch & Ended" + eventData);
    }

    public float Horizontal()
    {
        return touchPosition.x;
    }

    public float Vertical()
    {
        return touchPosition.y;
    }
}
