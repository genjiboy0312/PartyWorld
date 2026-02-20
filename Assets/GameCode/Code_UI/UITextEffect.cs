using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class UITextEffect : MonoBehaviour
{
    [SerializeField] private LoopType _lootType;
    [SerializeField] private Text _txt;
    void Start()
    {
        StartBlinking();
    }
    void StartBlinking()
    {
        // 텍스트 알파값을 0으로 줄였다가 1로 돌아오는 애니메이션을 무한 반복
        _txt.DOFade(0, 1f ).SetLoops(-1, _lootType) 
            .SetEase(Ease.InOutQuad); // 부드럽게 시작하고 끝나는 이징 설정
    }
}
