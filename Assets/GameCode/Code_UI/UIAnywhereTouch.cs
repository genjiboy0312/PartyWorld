using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAnywhereTouch : MonoBehaviour
{
    [SerializeField] private GameObject _uiLogin;
    [SerializeField] private Button _btnTouch;
    private void Start()
    {
        _btnTouch.onClick.AddListener(OpenLogin);
    }
    public void OpenLogin()
    {
        _uiLogin.SetActive(true);
    }
}