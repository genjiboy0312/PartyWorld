using UnityEngine;

/// <summary>
/// PlayerPresenter가 CameraStickController 같은 구체 클래스 대신
/// 의존할 수 있는 카메라 입력 인터페이스.
/// </summary>
public interface ICameraInputProvider
{
    /// <summary>카메라 회전용 델타 입력.</summary>
    Vector2 GetCameraDelta();

    /// <summary>프레임 소비 후 델타 초기화.</summary>
    void ResetCameraDelta();
}
