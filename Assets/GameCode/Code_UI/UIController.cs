using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Setting Music")]
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Button _musicBtn;

    [Header("Setting SFX")]
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Button _sfxBtn;

    [Header("Setting Exit")]
    [SerializeField] private GameObject _settingBoard;
    [SerializeField] private Button _exitBtn;

    private void Start()
    {
        //  이벤트 등록
        _musicBtn.onClick.AddListener(ToggleMusic);
        _sfxBtn.onClick.AddListener(ToggleSFX);
        _exitBtn.onClick.AddListener(Exit);

        _musicSlider.onValueChanged.AddListener(delegate { MusicVolume(); });
        _sfxSlider.onValueChanged.AddListener(delegate { SFXVolume(); });
    }

    private void ToggleMusic() => AudioManager.Instance.ToggleMusic();
    private void ToggleSFX() => AudioManager.Instance.ToggleSFX();
    private void MusicVolume() => AudioManager.Instance.MusicVolume(_musicSlider.value);
    private void SFXVolume() => AudioManager.Instance.SFXVolume(_sfxSlider.value);
    private void Exit() => _settingBoard.SetActive(false);
}
