using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputHandler : MonoBehaviour
{
    #region Singleton
    public static PlayerInputHandler Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keeps the instance alive across scenes
        }
    }
    #endregion

    //Movement
    private Vector2 _movementInputXY;

    //Jump
    public bool JumpButtonPressed;
    public bool JumpButtonReleased;
    private bool _isJumpButtonPressedThisFrame;

    //Dash
    private bool DashButtonPressed;
    private bool _isDashButtonPressedThisFrame;

    //Attack
    private bool AttackButtonPressed;

    public bool IsDashButtonPressedThisFrame()
    {
        if (_isDashButtonPressedThisFrame)
        {
            _isDashButtonPressedThisFrame = false;
            return true;
        }
        return false;
    }

    public bool IsJumpButtonPressedThisFrame()
    {
        if (_isJumpButtonPressedThisFrame)
        {
            _isJumpButtonPressedThisFrame = false;
            return true;
        }
        return false;
    }
    public bool IsPlayerPressingDownMovementButton()
    {
        if(GetMovementInput().y < 0)
            return true;

        return false;
    }
    public Vector2 GetMovementInput()
    {
        return _movementInputXY;
    }
    public int GetPlayerDirection()
    {
        if (transform.eulerAngles.y == 0)
            return 1; //right

        return -1; //left
    }

    #region Input Functions
        public void OnMovement(InputAction.CallbackContext context)
    {
        _movementInputXY = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            _isJumpButtonPressedThisFrame = true;
        }
        if (context.started)
        {
            JumpButtonPressed = true;
            JumpButtonReleased = false;
        }
        else if (context.canceled)
        {
            JumpButtonReleased = true;
            JumpButtonPressed = false;
        } 
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
            _isDashButtonPressedThisFrame = true;

        if (context.started)
            DashButtonPressed = true;
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
            AttackButtonPressed = true;
    }
    #endregion
}
