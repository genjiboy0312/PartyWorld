using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStatus : MonoBehaviour
{
    [SerializeField] private GameObject _camInventory;
    [SerializeField] private GameObject _btnOnOff;
    private bool _isCamChecking;
    private bool _isBtnChecking;

    public void OnOff()
    {
        _camInventory.SetActive(_isCamChecking = !_isCamChecking);
        _btnOnOff.SetActive(_isBtnChecking = !_isBtnChecking);
    }
}
