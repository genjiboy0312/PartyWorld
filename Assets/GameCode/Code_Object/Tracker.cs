using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tracker : MonoBehaviour
{
    [SerializeField] private Transform from, to;
    [SerializeField] private float tweenTime;

    [SerializeField] private Transform satellite;
    [SerializeField] private Transform satellite2;

    [SerializeField] private float _elapsedTime = 0f;        //  경과 시간


    void Update()
    {
        //  1.정해진 시간 tweenTime 동안 from -> to 를 선형으로 이동하는 Tracker 클래스를 구현하세요.
        if (_elapsedTime < tweenTime)
        {
            _elapsedTime += Time.deltaTime;
            //  시간 정규화
            float _time = Mathf.Clamp01(_elapsedTime / tweenTime);
            transform.position = Vector3.Lerp(from.position, to.position, _time);

            // 2. 이동하는 방향을 기준으로 좌측으로 수직하게 1만큼 떨어진 위치에 있는 satellite의 위치를 갱신
            //  거리 정규화
            Vector3 _dir = (to.position - from.position).normalized;
            // 이동 방향에 수직 이동한 위치
            Vector3 _sugic = new Vector3(_dir.y, -_dir.x - 1, 0f); //   이 부분은 잘 못됨
            //  이동하는 위치에 수직 이동한 벡터 더해줌
            Vector3 _satellitePos = transform.position + _sugic;
            //  위치 업데이트
            satellite.position = _satellitePos;
        }
        // 3. satellite2가 회전하도록 구현
        if (_elapsedTime < tweenTime)
        {
            // 5회 회전
            float _angle = 360f * 5f * (_elapsedTime / tweenTime);
            //  거리 3만큼 떨어짐
            satellite2.position = transform.position + new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), Mathf.Sin(_angle * Mathf.Deg2Rad), 0f) * 3f;
        }
    }

    //  다음 함수는 추가 구현 없이 사용하실 수 있습니다
    Vector3 Cross(Vector3 lhs, Vector3 rhs)
    {
        return Vector3.Cross(lhs, rhs);
    }

    Vector3 Normalize(Vector3 value)
    {
        return value.normalized;
    }

    float Sin(float f)
    {
        return Mathf.Sin(f);
    }

    float Cos(float f)
    {
        return Mathf.Cos(f);
    }
}