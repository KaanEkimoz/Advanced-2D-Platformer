using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using GlobalTypes;
public class PlayerController : MonoBehaviour
{
    [Header("Walk")]
    public float walkSpeed = 10f;
    [Space]
    [Header("Jump")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private float jumpSpeed = 15f;
    [Space]
    [Header("Double Jump")]
    [SerializeField] private bool canDoubleJump = true;
    [SerializeField] private float doubleJumpSpeed = 10f;
    [Space]
    [Header("Triple Jump")]
    [SerializeField] private bool canTripleJump = false;
    [SerializeField] private float tripleJumpSpeed = 10f;
    [Space]
    [Header("Wall Jump")]
    [SerializeField] private bool canWallJump = false;
    [SerializeField] private bool canJumpAfterWallJump = false;
    [SerializeField] private float wallJumpXSpeed = 15f;
    [SerializeField] private float wallJumpYSpeed = 15f;
    [Space]
    [Header("Wall Run")]
    [SerializeField] private bool canWallRun = false;
    [SerializeField] private bool canMultipleWallRun = false;
    [SerializeField] private float wallRunAmount = 8f;
    [Space]
    [Header("Wall Slide")]
    [SerializeField] private bool canWallSlide = false;
    [SerializeField] [Range(0f, 1f)] [Tooltip("Factor to multiply gravity with")] 
    private float wallSlideAmount = 0.1f;
    [Space]
    [Header("Glide")]
    [SerializeField] private bool canGlide;
    [SerializeField] private bool canGlideAfterWallContact;
    [SerializeField] private float glideTime = 2f;
    [SerializeField] private float glideDescentAmount = 2f;
    private float _currentGlideTime;

    [Header("Player State")]
    [SerializeField] private bool isJumping;
    [SerializeField] private bool isDoubleJumping;
    [SerializeField] private bool isTripleJumping;    
    [SerializeField] private bool isWallJumping;
    [SerializeField] private bool isWallRunning;
    [SerializeField] private bool isWallSliding;
    [SerializeField] private bool isGliding;
    [SerializeField] private bool isCreeping;
    [SerializeField] private bool isDucking;
    private bool ableToWallRun = true;

    

    [Header("Power Jump")]
    public float powerJumpSpeed = 40f;
    [SerializeField] private float powerJumpWaitTime = 1.5f;
    [SerializeField] private bool canPowerJump;
    [SerializeField] private bool isPowerJumping;
    private float _powerJumpTimer;

    [Header("Dash")]
    [SerializeField] private bool canGroundDash;
    [SerializeField] private bool canAirDash;
    public float dashSpeed = 20f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldownTime = 1f;
    
    [SerializeField] private bool isDashing;
    [SerializeField] private float _dashTimer;

    [Header("Ground Slam")]
    public float groundSlamSpeed = 60f;
    [SerializeField] private bool canGroundSlam;
    [SerializeField] private bool isGroundSlamming;

    [Header("Gravity")]
    [SerializeField] private float gravity = 20f;

    //Components
    private CharacterController2D _characterController;
    private CapsuleCollider2D _capsuleCollider;
    private SpriteRenderer _spriteRenderer;
    private Vector2 _originalColliderSize;

    //Input
    private bool _startJump;
    private bool _releaseJump;
    private bool _startGlide;
    private Vector2 _input;
    private Vector2 _moveDirection;
    private bool _facingRight;

    private void Start()
    {
        GetComponents();
        _originalColliderSize = _capsuleCollider.size;
    }
    private void GetComponents()
    {
        _characterController = gameObject.GetComponent<CharacterController2D>();
        _capsuleCollider = gameObject.GetComponent<CapsuleCollider2D>();
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }
    private void Update()
    {
        if (_dashTimer > 0)
            _dashTimer -= Time.deltaTime;

        if (!isWallJumping)
        {
            _moveDirection.x = _input.x;
            _moveDirection.x *= walkSpeed;

            if (_moveDirection.x < 0)
            {
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                _facingRight = false;
            }
            else if (_moveDirection.x > 0)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                _facingRight = true;
            }    
        }
        if (isDashing)
        {
            if (_facingRight)
                _moveDirection.x = dashSpeed;
            else
                _moveDirection.x = -dashSpeed;

            _moveDirection.y = 0;
        }
        else
            _moveDirection.x *= walkSpeed;

        //On the ground
        if (_characterController.below)
        {
            _moveDirection.y = 0f;

            //clear flags for in air abilities
            isJumping = false;
            isDoubleJumping = false;
            isTripleJumping = false;
            isWallJumping = false;
            _currentGlideTime = glideTime;
            isGroundSlamming = false;

            //jumping
            if (_startJump)
            {
                _startJump = false;

                if (canPowerJump && isDucking &&
                    _characterController.groundType != GroundType.OneWayPlatform && (_powerJumpTimer > powerJumpWaitTime))
                {
                    _moveDirection.y = powerJumpSpeed;
                    StartCoroutine("PowerJumpWaiter");
                }
                else
                    _moveDirection.y = jumpSpeed;

                isJumping = true;
                _characterController.DisableGroundCheck();
                ableToWallRun = true;
            }
            //ducking and creeping
            if (_input.y < 0f)
            {
                if (!isDucking && !isCreeping)
                {
                    _capsuleCollider.size = new Vector2(_capsuleCollider.size.x, _capsuleCollider.size.y / 2);
                    transform.position = new Vector2(transform.position.x, transform.position.y - (_originalColliderSize.y / 4));
                    isDucking = true;
                    _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp_crouching");
                }
                _powerJumpTimer += Time.deltaTime;
            }
            else
            {
                if (isDucking || isCreeping)
                {
                    RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_capsuleCollider.bounds.center,
                         transform.localScale, CapsuleDirection2D.Vertical, 0f, Vector2.up,
                         _originalColliderSize.y / 2, _characterController.layerMask);

                    if (!hitCeiling.collider)
                    {
                        _capsuleCollider.size = _originalColliderSize;
                        transform.position = new Vector2(transform.position.x, transform.position.y + (_originalColliderSize.y / 4));
                        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");
                        isDucking = false;
                        isCreeping = false;
                    }
                }
            }
            if (isDucking && _moveDirection.x != 0)
            {
                isCreeping = true;
                _powerJumpTimer = 0f;
            }
            else
                isCreeping = false;
        }
        // In the air
        else
        {
            if ((isDucking || isCreeping) && _moveDirection.y > 0)
                StartCoroutine("ClearDuckingState");

            _powerJumpTimer = 0f;

            if (_releaseJump)
            {
                _releaseJump = false;

                if (_moveDirection.y > 0)
                    _moveDirection.y *= 0.5f;
            }

            if (_startJump)
            {
                // triple jump
                if (canTripleJump && (!_characterController.left && !_characterController.right))
                {
                    if (isDoubleJumping && !isTripleJumping)
                    {
                        _moveDirection.y = tripleJumpSpeed;
                        isTripleJumping = true;
                    }
                }
                // double jump
                if (canDoubleJump && (!_characterController.left && !_characterController.right))
                {
                    if (!isDoubleJumping)
                    {
                        _moveDirection.y = doubleJumpSpeed;
                        isDoubleJumping = true;
                    }
                }
                //wall jump
                if (canWallJump && (_characterController.left || _characterController.right))
                {
                    if (_moveDirection.x <= 0 && _characterController.left)
                    {
                        _moveDirection.x = wallJumpXSpeed;
                        _moveDirection.y = wallJumpYSpeed;
                        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                    else if (_moveDirection.x >= 0 && _characterController.right)
                    {
                        _moveDirection.x = -wallJumpXSpeed;
                        _moveDirection.y = wallJumpYSpeed;
                        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    }

                    StartCoroutine("WallJumpWaiter");

                    if (canJumpAfterWallJump)
                    {
                        isDoubleJumping = false;
                        isTripleJumping = false;
                    }
                    _startJump = false;
                }
            }

            //wall running
            if (canWallRun && (_characterController.left || _characterController.right))
            {
                if (_input.y > 0 && ableToWallRun)
                {
                    _moveDirection.y = wallRunAmount;

                    if (_characterController.left)
                        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    else if (_characterController.right)
                        transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                    StartCoroutine("WallRunWaiter");
                }
            }
            else
            {
                if (canMultipleWallRun)
                {
                    StopCoroutine("WallRunWaiter");
                    ableToWallRun = true;
                    isWallRunning = false;
                }
            }
            GravityCalculations();

            //canGlideAfterWallContact
            if ((_characterController.left || _characterController.right) && canWallRun)
            {
                if (canGlideAfterWallContact)
                {
                    _currentGlideTime = glideTime;
                }
                else
                {
                    _currentGlideTime = 0;
                }
            }
        }
        _characterController.Move(_moveDirection * Time.deltaTime);
    }
    private bool IsCharacterOnTheGround()
    {
        return _characterController.below;
    }
    void GravityCalculations()
    {
        //detects if something above player
        if (_moveDirection.y > 0f && _characterController.above)
        {
            _moveDirection.y = 0f;
        }

        //apply wall slide adjustment
        if (canWallSlide && (_characterController.left || _characterController.right))
        {
            if (_characterController.hitWallThisFrame)
            {
                _moveDirection.y = 0;
            }


            if (_moveDirection.y <= 0)
            {
                _moveDirection.y -= (gravity * wallSlideAmount) * Time.deltaTime;
            }
            else
            {
                _moveDirection.y -= gravity * Time.deltaTime;
            }

        }
        else if (canGlide && _input.y > 0f && _moveDirection.y < 0.2f) // glide adjustment
        {
            if (_currentGlideTime > 0f)
            {
                isGliding = true;

                if (_startGlide)
                {
                    _moveDirection.y = 0;
                    _startGlide = false;
                }

                _moveDirection.y -= glideDescentAmount * Time.deltaTime;
                _currentGlideTime -= Time.deltaTime;
            }
            else
            {
                isGliding = false;
                _moveDirection.y -= gravity * Time.deltaTime;
            }

        }
        //else if (canGroundSlam  && !isPowerJumping && _input.y < 0f && _moveDirection.y < 0f) // ground slam
        else if (isGroundSlamming && !isPowerJumping && _moveDirection.y < 0f)
        {
            _moveDirection.y = -groundSlamSpeed;
        }
        else if (!isDashing) //regular gravity
        {
            _moveDirection.y -= gravity * Time.deltaTime;
        }
    }

    //Input Methods
    public void OnMovement(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _startJump = true;
            _releaseJump = false;
        }
        else if (context.canceled)
        {
            _releaseJump = true;
            _startJump = false;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started && _dashTimer <= 0)
        {
            if ((canAirDash && !_characterController.below)
                || (canGroundDash && _characterController.below))
            {
                StartCoroutine("Dash");
            }
        }
    }
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && _input.y < 0f)
        {
            if (canGroundSlam)
            {
                isGroundSlamming = true;
            }
        }
    }

    #region Coroutines
    IEnumerator WallJumpWaiter()
    {
        isWallJumping = true;
        yield return new WaitForSeconds(0.4f);
        isWallJumping = false;
    }
    IEnumerator WallRunWaiter()
    {
        isWallRunning = true;
        yield return new WaitForSeconds(0.5f);
        isWallRunning = false;
        if (!isWallJumping)
            ableToWallRun = false;
    }
    IEnumerator ClearDuckingState()
    {
        yield return new WaitForSeconds(0.05f);

        RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, transform.localScale,
            CapsuleDirection2D.Vertical, 0f, Vector2.up, _originalColliderSize.y / 2, _characterController.layerMask);

        if (!hitCeiling.collider)
        {
            _capsuleCollider.size = _originalColliderSize;
            _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");
            isDucking = false;
            isCreeping = false;
        }
    }
    IEnumerator PowerJumpWaiter()
    {
        isPowerJumping = true;
        yield return new WaitForSeconds(0.8f);
        isPowerJumping = false;
    }
    IEnumerator Dash()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        _dashTimer = dashCooldownTime;
    }
    #endregion
}

