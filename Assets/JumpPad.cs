using UnityEngine;
public class JumpPad : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 40.0f;
    [SerializeField] private float cooldownTime = 3.0f;

    private float jumpPadAngle; //Same as launch direction

    public Vector2 GetLaunchVector()
    {
        return GetJumpPadAngle() * launchSpeed;
    }
    private Vector2 GetJumpPadAngle()
    {
        Vector2 jumpPadDirection = new Vector2(Mathf.Cos(jumpPadAngle * Mathf.Deg2Rad), Mathf.Sin(jumpPadAngle * Mathf.Deg2Rad));
        return jumpPadDirection;
    }
}
