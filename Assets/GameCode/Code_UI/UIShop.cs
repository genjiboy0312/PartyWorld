using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIShop : MonoBehaviour
{
    [SerializeField] private GameObject _shopBoard;
    [SerializeField] private Button _exitBtn;
    private void Start() => _exitBtn.onClick.AddListener(Exit);

    private void Exit() => _shopBoard.SetActive(false);
}
