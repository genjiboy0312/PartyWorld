using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UITextTypingMotion : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Text _txtLoading;
    [SerializeField] private string _loadingText = "Loading....";

    private Coroutine _typingCoroutine;

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

            // 텍스트를 한 글자씩 타이핑
            _txtLoading.text = string.Empty;
            int totalChars = _loadingText.Length;
            float typingSpeed = totalChars > 0 ? 2.5f / totalChars : 0.1f;
            for (int i = 0; i <= totalChars; i++)
            {
                _txtLoading.text = _loadingText.Substring(0, i);
                yield return new WaitForSeconds(typingSpeed);
            }
            // 텍스트 완료 후 잠깐 대기
            yield return new WaitForSeconds(1f);
        }
    }

    private void OnDisable()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }
}
