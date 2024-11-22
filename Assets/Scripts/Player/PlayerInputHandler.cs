using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputHandler : MonoBehaviour
{
    #region Singleton
    public static PlayerInputHandler Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    #endregion

    public bool JumpButtonPressed { get { return IsJumpButtonPressedThisFrame(); } }
    public bool JumpButtonHeld { get { return _jumpButtonHeld; } }

    //Movement
    private Vector2 _moveInput;

    //Jump
    private bool _jumpButtonHeld;
    private bool _jumpButtonPressedThisFrame;

    //Dash
    private bool _isDashButtonPressedThisFrame;

    //Attack
    private bool _isAttackButtonPressedThisFrame;

    private bool IsDashButtonPressedThisFrame()
    {
        if (_isDashButtonPressedThisFrame)
        {
            _isDashButtonPressedThisFrame = false;
            return true;
        }
        return false;
    }
    private bool IsJumpButtonPressedThisFrame()
    {
        if (_jumpButtonPressedThisFrame)
        {
            _jumpButtonPressedThisFrame = false;
            return true;
        }
        return false;
    }
    private bool IsPlayerPressingDownMovementButton()
    {
        if(GetMovementInput().y < 0)
            return true;

        return false;
    }
    private bool IsAttackButtonPressedThisFrame()
    {
        if (_isAttackButtonPressedThisFrame)
        {
            _isAttackButtonPressedThisFrame = false;
            return true;
        }
        return false;
    }
    public Vector2 GetMovementInput()
    {
        return _moveInput;
    }
    public int GetPlayerDirection()
    {
        if (transform.eulerAngles.y == 0)
            return 1; //right

        return -1; //left
    }

    #region Input Event Functions
    public void OnMovement(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _jumpButtonPressedThisFrame = true;
            _jumpButtonHeld = true;
        }
        else if(context.canceled)
            _jumpButtonHeld = false;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
            _isDashButtonPressedThisFrame = true;
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
            _isAttackButtonPressedThisFrame = true;
    }
    #endregion
}
