using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIInventory : MonoBehaviour
{
    [SerializeField] private GameObject _camInventory;
    [SerializeField] private GameObject _inventoryBoard;
    [SerializeField] private Button _exitBtn;

    private bool _isCamChecking;

    private void Start() => _exitBtn.onClick.AddListener(Exit);
    public void OnOff() => _camInventory.SetActive(_isCamChecking = !_isCamChecking);
    private void Exit() => _inventoryBoard.SetActive(false);
}
