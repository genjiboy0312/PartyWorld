using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPlay : MonoBehaviour
{
    [Header("Setting UI Play")]
    [SerializeField] private GameObject _playSectUI;

    [SerializeField] private Button _playBtn;
    [SerializeField] private Button _yesBtn;
    [SerializeField] private Button _noBtn;
    private void Start()
    {
        _playBtn.onClick.AddListener(PlayButton);
        _yesBtn.onClick.AddListener(YesButton);
        _noBtn.onClick.AddListener(NoButton);
    }

    //  play btn을 누르면 yes btn이 뜸
    private void PlayButton() => _playSectUI.SetActive(true);
    //  PlayButton. Loading Scene or Loading UI 뜨게 함 (고민)
    private void YesButton() => SceneManager.LoadScene("Scene_Loading");
    private void NoButton() => _playSectUI.SetActive(false);
}