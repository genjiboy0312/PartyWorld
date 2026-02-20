using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Cool Time Check")]
    [SerializeField] private List<Image> _imageCoolTime;
    [SerializeField] private List<bool> _coolTimeCheck;
    private List<UICoolTime> _coolTimeScripts;

    void Start()
    {
        _coolTimeScripts = new List<UICoolTime>(_imageCoolTime.Count);
        _coolTimeCheck = new List<bool>(_imageCoolTime.Count);

        for (int i = 0; i < _imageCoolTime.Count; i++)
        {
            UICoolTime _coolTimeScript = _imageCoolTime[i].GetComponent<UICoolTime>();
            _coolTimeScripts.Add(_coolTimeScript);
            _coolTimeCheck.Add(false);
        }
    }

    // 스킬 쿨타임 체크용 메소드
    public void SetCoolTimeCheck(int index, bool value)
    {
        if (index >= 0 && index < _coolTimeScripts.Count)
        {
            _coolTimeCheck[index] = value;
        }
    }
    //public int GetButtonIndex(UICoolTime button)
    //{
    //    for (int i = 0; i < _coolTimeScripts.Count; i++)
    //    {
    //        if (_coolTimeScripts[i] == button)
    //        {
    //            return i;
    //        }
    //    }
    //    return -1;
    //}
}
