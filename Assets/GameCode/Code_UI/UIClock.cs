using UnityEngine;
using UnityEngine.UI;

public class UIClock : MonoBehaviour
{
    [SerializeField] private Text _clockText;
    private void Update()
    {
        _clockText.text = System.DateTime.Now.ToString("MM월 dd일 HH시 mm분");
    }
}
