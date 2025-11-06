using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private string _playerTag = "Player"; // 플레이어를 식별하기 위한 태그
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Vector3 _offset;

    void FixedUpdate()
    {
        GameObject _playerObj = GameObject.FindGameObjectWithTag(_playerTag);
        // 플레이어 오브젝트를 찾았다면 해당 트랜스폼을 저장합니다.
        if (_playerObj != null)
        {
            _playerTransform = _playerObj.transform;
        }

        // 플레이어를 찾았다면 위치를 업데이트합니다.
        if (_playerTransform != null)
        {
            transform.position = _playerTransform.position + _offset;
        }
    }
}
