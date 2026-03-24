using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UITextTypingMotion : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Text _txtLoading;          // 로딩 텍스트 UI
    [SerializeField] private string _loadingText = "Loading...."; // 반복할 텍스트

    private Coroutine _typingCoroutine;
    private Tweener _typingTween;

    private void Start()
    {
        if (_txtLoading != null)
            _typingCoroutine = StartCoroutine(TypingLoop());
        else
            Debug.LogWarning("UITextTypingMotion: _txtLoading이 할당되지 않았습니다.");
    }

    private IEnumerator TypingLoop()
    {
        while (true)
        {
            if (_txtLoading == null)
                yield break;

            _txtLoading.text = string.Empty;

            // DOTween을 사용하여 텍스트 타이핑 애니메이션
            _txtLoading.DOKill();
            _typingTween?.Kill();

            _typingTween = _txtLoading.DOText(_loadingText, 2.5f);
            yield return _typingTween.WaitForCompletion();

            // 텍스트 애니메이션 후 잠깐 대기
            yield return new WaitForSeconds(1f);
        }
    }

    private void OnDisable()
    {
        if (_typingTween != null)
        {
            _typingTween.Kill();
            _typingTween = null;
        }

        if (_txtLoading != null)
            _txtLoading.DOKill();

        // 씬 전환 등에서 Coroutine 종료
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (_typingTween != null)
        {
            _typingTween.Kill();
            _typingTween = null;
        }
    }
}
