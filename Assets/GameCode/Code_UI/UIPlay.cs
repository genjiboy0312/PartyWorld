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

    //  play btnÀ» ´©¸£¸é yes btnÀÌ ¶ä
    private void PlayButton() => _playSectUI.SetActive(true);
    //  PlayButton. Loading Scene or Loading UI ¶ß°Ô ÇÔ (°í¹Î)
    private void YesButton() => SceneManager.LoadScene("Scene_Loading");
    private void NoButton() => _playSectUI.SetActive(false);
}