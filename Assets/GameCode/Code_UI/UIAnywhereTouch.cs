using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIAnywhereTouch : MonoBehaviour
{
    [SerializeField] private GameObject _uiLogin;
    public void OpenLogin()
    {
        _uiLogin.SetActive(true);
    }
}