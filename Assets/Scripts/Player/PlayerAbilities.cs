using GlobalTypes;
using System.Collections;
using UnityEngine;
public class PlayerAbilities : MonoBehaviour
{
    [Header("Glide")]
    [SerializeField] private bool canGlide;
    [SerializeField] private bool canGlideAfterWallContact;
    [SerializeField] private float glideTime = 2f;
    [SerializeField] private float glideDescentAmount = 2f;
    [Space]
    [Header("Power Jump")]
    [SerializeField] private bool canPowerJump;
    [SerializeField] private float powerJumpSpeed = 40f;
    [SerializeField] private float powerJumpWaitTime = 1.5f;
    [Space]
    [Header("Dash")]
    [SerializeField] private bool canGroundDash;
    [SerializeField] private bool canAirDash;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldownTime = 1f;
    [Space]
    [Header("Ground Slam")]
    [SerializeField] private bool canGroundSlam;
    [SerializeField] private float groundSlamSpeed = 60f;

    //Player States
    private bool isGliding;
    private bool isPowerJumping;
    private bool isDashing;
    private bool isGroundSlamming;

    //Glide
    private bool _startGlide = true;
    private float _currentGlideTime;

    //Dash
    private float _dashTimer;

    //Power Jump
    public float _powerJumpTimer;

    //Player Movement
    private PlayerMovement _playerMovement;
    private AdvancedCharacterCollision2D _characterCollision2D;
    private Vector2 _targetMoveDirection;

    private void Start()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _characterCollision2D = GetComponent<AdvancedCharacterCollision2D>();
        _currentGlideTime = glideTime;
    }
    private void Update()
    {
        RunDashTimer();

        RunPowerJumpTimer();
        
        if(canPowerJump  && _playerMovement._inputMovementVector.y > 0 && _characterCollision2D.groundType != GroundType.OneWayPlatform && 
            (_powerJumpTimer > powerJumpWaitTime))
            StartCoroutine(nameof(PowerJumpWaiter));

        if(PlayerInputHandler.Instance.IsAttackButtonPressedThisFrame() && !isPowerJumping && _playerMovement._inputMovementVector.y <= 0f )
            isGroundSlamming = true;

        if (PlayerInputHandler.Instance.IsDashButtonPressedThisFrame())
            StartCoroutine(nameof(StartDashing));

        if(canGlide && PlayerInputHandler.Instance.GetMovementInput().y > 0 && _playerMovement._inputMovementVector.y < 0.2f && _currentGlideTime > 0f)
            isGliding = true;
        else
            isGliding = false;

        if (isGroundSlamming)
            _playerMovement.MoveThePlayer(Vector2.down * groundSlamSpeed * Time.deltaTime);

        if (isDashing)
            _playerMovement.MoveThePlayer(new Vector2(PlayerInputHandler.Instance.GetPlayerDirection() * dashSpeed * Time.deltaTime, 0));

        if(isPowerJumping)
            _playerMovement.MoveThePlayer(Vector2.up * powerJumpSpeed * Time.deltaTime);

        if (isGliding)
        {
            _playerMovement.ResetVerticalMovement();
            _playerMovement.MoveThePlayer(Vector2.down * glideDescentAmount * Time.deltaTime);
            _currentGlideTime -= Time.deltaTime;
        }
            
        if (_characterCollision2D.IsGrounded())
        {
            isGroundSlamming = false;
            ResetGlideTimer();
        }

        if (canGlideAfterWallContact && HasWallContact())
            ResetGlideTimer();
    }
    private void ResetGlideTimer()
    {
        _currentGlideTime = glideTime;
    }
    private void RunDashTimer()
    {
        if (_dashTimer > 0)
            _dashTimer -= Time.deltaTime;
    }
    private void RunPowerJumpTimer()
    {
        if (PlayerInputHandler.Instance.GetMovementInput().y < 0 && PlayerInputHandler.Instance.GetMovementInput().x == 0)
            _powerJumpTimer += Time.deltaTime;
        else
            _powerJumpTimer = 0;
    }
    private bool HasWallContact()
    {
        return _characterCollision2D.left || _characterCollision2D.right;
    }
    #region Coroutines
    IEnumerator PowerJumpWaiter()
    {
        isPowerJumping = true;
        yield return new WaitForSeconds(0.8f);
        _powerJumpTimer = 0;
        isPowerJumping = false;
        
    }
    IEnumerator StartDashing()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        _dashTimer = dashCooldownTime;
    }
    #endregion
}
