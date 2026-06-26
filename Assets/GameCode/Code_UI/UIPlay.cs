using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPlay : MonoBehaviour
{
    [Header("Setting UI Play")]
    [SerializeField] private GameObject _playSectUI;
    [SerializeField] private UIRoomSelection _roomSelectionUI;

    [SerializeField] private Button _playBtn;
    [SerializeField] private Button _yesBtn;
    [SerializeField] private Button _noBtn;
    private void Start()
    {
        _playBtn.onClick.AddListener(PlayButton);
        _yesBtn.onClick.AddListener(YesButton);
        _noBtn.onClick.AddListener(NoButton);
    }

    //  play btn을 누르면 room selection UI가 뜸
    private void PlayButton()
    {
        if (_roomSelectionUI != null)
        {
            _roomSelectionUI.Open();
            return;
        }

        _playSectUI.SetActive(true);
    }
    //  WaitingRoom에서 QuickPlay를 시작(룸 매칭 진입)
    private void YesButton()
    {
        if (NetworkAuthorityManager.Instance != null)
        {
            NetworkAuthorityManager.Instance.StartQuickPlay();
            _playSectUI.SetActive(false);
            return;
        }
    }
    private void NoButton() => _playSectUI.SetActive(false);
}
