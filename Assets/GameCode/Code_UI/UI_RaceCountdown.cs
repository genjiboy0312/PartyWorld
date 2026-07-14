using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레이스 시작 전 카운트다운(10…1 → Ready… → Start!)을 표시합니다.
/// WaitingRoomUIController의 PlayCountdownPop과 동일한 팝 스케일 애니메이션을 사용합니다.
/// </summary>
public class UI_RaceCountdown : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text _countdownText;

    [Header("Pop Animation")]
    [SerializeField] private float _popScale = 1.35f;
    [SerializeField] private float _popDuration = 0.18f;
    [SerializeField] private float _shrinkDuration = 0.12f;
    [SerializeField] private float _startScale = 0.85f;

    [Header("Timing")]
    [SerializeField] private float _readyDuration = 0.6f;
    [SerializeField] private float _startDuration = 1.2f;

    private Coroutine _animCoroutine;

    private void Awake()
    {
        if (_countdownText == null)
            _countdownText = GetComponent<Text>();

        gameObject.SetActive(false);
    }

    /// <summary>카운트다운 숫자(10~1)를 표시합니다.</summary>
    public void ShowNumber(int number)
    {
        gameObject.SetActive(true);
        _countdownText.text = number.ToString();
        PlayPop();
    }

    /// <summary>"Ready…" 텍스트를 표시합니다.</summary>
    public void ShowReady()
    {
        gameObject.SetActive(true);
        _countdownText.text = "Ready....";
        PlayPop();
    }

    /// <summary>"Start!" 텍스트를 표시합니다.</summary>
    public void ShowStart()
    {
        gameObject.SetActive(true);
        _countdownText.text = "Start !!!";
        PlayPop();
    }

    /// <summary>카운트다운 UI를 숨깁니다.</summary>
    public void Hide()
    {
        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
            _animCoroutine = null;
        }

        if (_countdownText != null)
        {
            _countdownText.transform.localScale = Vector3.one;
            _countdownText.text = string.Empty;
        }

        gameObject.SetActive(false);
    }

    private void PlayPop()
    {
        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = StartCoroutine(PlayPopAnimation());
    }

    private IEnumerator PlayPopAnimation()
    {
        if (_countdownText == null)
            yield break;

        RectTransform rt = _countdownText.rectTransform;
        if (rt == null)
            yield break;

        float start = Mathf.Max(0.01f, _startScale);
        float pop = Mathf.Max(start, _popScale);
        float popDur = Mathf.Max(0.01f, _popDuration);
        float shrinkDur = Mathf.Max(0.01f, _shrinkDuration);

        // 커졌다가 다시 원래 크기로 돌아오는 "팝" 효과
        float t = 0f;
        while (t < popDur)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / popDur);
            float s = Mathf.Lerp(start, pop, a);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        t = 0f;
        while (t < shrinkDur)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / shrinkDur);
            float s = Mathf.Lerp(pop, 1f, a);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        rt.localScale = Vector3.one;
        _animCoroutine = null;
    }

    /// <summary>Ready → Start 전환을 위한 대기 시간 Getter (외부 코루틴용).</summary>
    public float ReadyDuration => _readyDuration;
    public float StartDuration => _startDuration;
}
