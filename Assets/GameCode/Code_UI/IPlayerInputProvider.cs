using UnityEngine;

/// <summary>
/// PlayerPresenter가 JoyStickController 같은 구체 클래스 대신
/// 의존할 수 있는 이동 입력 인터페이스.
/// </summary>
public interface IPlayerInputProvider
{
    /// <summary>정규화된 이동 입력 (-1 ~ 1).</summary>
    Vector2 GetMoveInput();
}
