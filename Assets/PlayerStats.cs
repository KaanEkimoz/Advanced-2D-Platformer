using UnityEngine;

[CreateAssetMenu]
public class PlayerStats : ScriptableObject
{
    [Header("MOVEMENT")]
    [Tooltip("The top horizontal movement speed")]
    public float MaxSpeed = 14;

    [Tooltip("The top horizontal movement speed")]
    public float CrouchSpeed = 7;

    [Tooltip("The player's capacity to gain horizontal speed")]
    public float Acceleration = 120;

    [Tooltip("The pace at which the player comes to a stop")]
    public float GroundDeceleration = 60;

    [Tooltip("Deceleration in air only after stopping input mid-air")]
    public float AirDeceleration = 30;

    [Tooltip("A constant downward force applied while grounded. Helps on slopes"), Range(0f, -10f)]
    public float GroundedForce = -1.5f;

    [Tooltip("The detection distance for grounding and roof detection"), Range(0f, 0.5f)]
    public float GrounderDistance = 0.05f;

    [Tooltip("The maximum angle the player can move up. 0 is flat, 90 is vertical")]
    public float SlopeAngleLimit = 45f;

    [Header("JUMP")]
    [Tooltip("The immediate velocity applied when jumping")]
    public float JumpPower = 36;

    [Tooltip("The immediate velocity applied when double jumping")]
    public float DoubleJumpPower = 28;

    [Tooltip("The immediate velocity applied when triple jumping")]
    public float TripleJumpPower = 20;

    [Tooltip("The immediate horizontal velocity applied when power jumping")]
    public float WallJumpHorizontalPower = 25;

    [Tooltip("The immediate vertical velocity applied when power jumping")]
    public float WallJumpVerticalPower = 25;

    [Tooltip("The immediate vertical velocity applied when power jumping")] [Range(0, 1)]
    public float WallSlideAmount = 0.3f;

    [Tooltip("Rotates the player after executed wall jump")]
    public bool AutoRotateAfterWallJump = true;

    [Tooltip("Resets double and triple jumps after wall jump")]
    public bool ResetMultipleJumpsAfterWallJump = true;

    [Tooltip("The maximum vertical movement speed")]
    public float MaxFallSpeed = 40;

    [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
    public float FallAcceleration = 110;

    [Tooltip("The gravity multiplier added when jump is released early")]
    public float JumpEndEarlyGravityModifier = 3;

    [Tooltip("The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
    public float CoyoteTime = .15f;

    [Tooltip("The amount of time we buffer a jump. This allows jump input before actually hitting the ground")]
    public float JumpBuffer = .2f;

    [Header("INPUT")]
    [Tooltip("Makes all Input snap to an integer. Prevents gamepads from walking slowly. Recommended value is true to ensure gamepad/keybaord parity.")]
    public bool SnapInput = true;

    [Tooltip("Minimum input required before you mount a ladder or climb a ledge. Avoids unwanted climbing using controllers"), Range(0.01f, 0.99f)]
    public float VerticalDeadZoneThreshold = 0.3f;

    [Tooltip("Minimum input required before a left or right is recognized. Avoids drifting with sticky controllers"), Range(0.01f, 0.99f)]
    public float HorizontalDeadZoneThreshold = 0.1f;

    [Header("ABILITIES")]
    [Tooltip("Allows the player to double jump")]
    public bool CanDoubleJump = true;

    [Tooltip("Allows the player to triple jump")]
    public bool CanTripleJump = false;

    [Tooltip("Allows the player to perform a powerful jump")]
    public bool CanPowerJump = false;

    [Header("LAYERS")]
    [Tooltip("Set this to the layer your player is on")]
    public LayerMask PlayerLayer;

    //[Header("ABILITIES")]

}
