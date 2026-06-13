using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UITextEffect : MonoBehaviour
{
    [SerializeField] private Text _txt;
    private Coroutine _blinkCoroutine;

    void Start()
    {
        StartBlinking();
    }

    void StartBlinking()
    {
        if (_txt == null)
        {
            Debug.LogWarning("UITextEffect: _txt가 할당되지 않았습니다.");
            return;
        }

        _blinkCoroutine = StartCoroutine(BlinkLoop());
    }

    private IEnumerator BlinkLoop()
    {
        Color originalColor = _txt.color;
        while (true)
        {
            float elapsed = 0f;
            float duration = 1f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float alpha = Mathf.PingPong(t * 2f, 1f);
                _txt.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void OnDisable()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
    }
}
