using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreCollision : MonoBehaviour
{
    [SerializeField] private Collider _collider;                //  최상단 캐릭터 오브젝트 넣기
    [SerializeField] private Collider[] _ignoreCollider;        //  무시할 콜라이더 오브젝트
    void Start()
    {
        foreach (Collider collider in _ignoreCollider)
        {
            Physics.IgnoreCollision(_collider, collider, true);
        }

    }
}
