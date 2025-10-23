using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ToggleButtonColor : MonoBehaviour
{
    private Button _button;
    private Color _originalColor;
    private Image _image;
    private bool _isToggled = false;

    [SerializeField] private Color _toggledColor = Color.gray; // 회색으로 토글될 색

    void Start()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();

        _originalColor = _image.color;
        _button.onClick.AddListener(ToggleColor);
    }

    void ToggleColor()
    {
        _image.color = _isToggled ? _originalColor : _toggledColor;
        _isToggled = !_isToggled;
    }
}
