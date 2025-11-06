using UnityEngine;

[System.Serializable]
public class PlayerModel
{
    public float Speed = 30f;
    public float JumpPower = 20f;

    public bool IsJump;
    public bool IsDive;
    public Vector3 MoveDirection;

    public void ResetStates()
    {
        IsJump = false;
        IsDive = false;
    }
}
