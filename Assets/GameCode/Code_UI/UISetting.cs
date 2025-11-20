using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISetting :MonoBehaviour
{
    [SerializeField] private GameObject _menuSetting;
    [SerializeField] private Button _btnSetting;
    private bool _isChecking;

    private void Start()
    {
        if (_btnSetting == null || _menuSetting == null)
        {
            Debug.LogError("UISetting: Button or MenuSetting Null");
            enabled = false;
            return;
        }

        _btnSetting.onClick.AddListener(OnOff);
    }

    public void OnOff() => _menuSetting.SetActive(_isChecking = !_isChecking);

}
