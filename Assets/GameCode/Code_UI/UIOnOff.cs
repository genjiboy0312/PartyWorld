using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIOnOff : MonoBehaviour
{
    [SerializeField] private GameObject _btnOnOff;
    private bool _isChecking;

    public void OnOff()
    {
        _btnOnOff.SetActive(_isChecking = !_isChecking);
    }
}
