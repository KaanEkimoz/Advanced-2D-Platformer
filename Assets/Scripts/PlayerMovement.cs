using System;
using System.Collections;
using UnityEngine;
using GlobalTypes;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
public class PlayerMovement : MonoBehaviour, IPlayerController
{
    [Header("Walk and Crouch Walk")]
    [SerializeField] private float walkSpeed = 10f;
    [SerializeField] private float crouchWalkSpeed = 5f;
    [Space]
    [Header("Slope Walk")]
    [SerializeField] private float downForceAdjustment = 1.2f;
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
    [SerializeField] private bool autoRotatePlayerAfterWallJump = true;
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
    [Header("Gravity")]
    [SerializeField] private float gravity = 20f;
    [Space]
    [Header("Player State")]
    [SerializeField] public bool _isJumping;
    [SerializeField] private bool _isDoubleJumping;
    [SerializeField] private bool _isTripleJumping;    
    [SerializeField] private bool isWallJumping;
    [SerializeField] private bool isWallRunning;
    [SerializeField] private bool isWallSliding;
    [SerializeField] private bool isGliding;
    [SerializeField] private bool isCrouchWalking;
    [SerializeField] private bool isCrouching;

    //Movement
    [HideInInspector] public Vector2 _inputMovementVector;
    [HideInInspector]public Vector2 _otherMovementVector;

    //Components
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rb;
    private Vector2 _originalColliderSize;

    [SerializeField] private PlayerStats _stats;
    
    private CapsuleCollider2D _col;
    private FrameInput _frameInput;
    private Vector2 _frameVelocity;

    public Vector2 FrameInput => _frameInput.Move;
    public event Action<bool, float> GroundedChanged;
    public event Action Jumped;

    private bool _jumpInputToApply;
    private float _time;

    private void Awake()
    {
        _col = GetComponent<CapsuleCollider2D>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalColliderSize = _col.size;
    }
    private void Update()
    {
        _time += Time.deltaTime;
        GatherInput();
    }
    private void FixedUpdate()
    {
        CheckCollisions();

        HandleJump();
        HandleHorizontalMovement();
        HandleDirection();
        HandleGravity();

        ApplyMovement();
    }

    private void GatherInput()
    {
        _frameInput = new FrameInput
        {
            JumpDown = PlayerInputHandler.Instance.IsJumpButtonPressedThisFrame(),
            JumpHeld = PlayerInputHandler.Instance.IsPressingJumpButton,
            Move = PlayerInputHandler.Instance.GetMovementInput()
        };

        if (_stats.SnapInput)
        {
            _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
            _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
        }

        if (_frameInput.JumpDown)
        {
            _jumpInputToApply = true;
            _timeJumpWasPressed = _time;
        }
    }

    #region Collisions

    public bool groundHit;
    public bool ceilingHit;
    public bool leftHit;
    public bool rightHit;

    private float _frameLeftGrounded = float.MinValue;
    private bool _grounded;

    private float _slopeAngle;
    private Vector2 _slopeNormal;

    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;

        // Ground and Ceiling
        RaycastHit2D groundHitRay = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);

        if (groundHitRay.collider)
            groundHit = true;
        else
            groundHit = false;

        //bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
        bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

        // Left and Right
        bool leftHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.left, _stats.GrounderDistance, ~_stats.PlayerLayer);
        bool rightHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.right, _stats.GrounderDistance, ~_stats.PlayerLayer);

        
        // Hit a Ceiling
        if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

        if (groundHit)
        {
            //groundType = DetectGroundType(hit.collider);
            //_groundCollisionObject = hit.collider.gameObject;

            _slopeNormal = groundHitRay.normal;
            _slopeAngle = Vector2.SignedAngle(_slopeNormal, Vector2.up);

            if (_slopeAngle > _stats.slopeAngleLimit || _slopeAngle < -_stats.slopeAngleLimit)
                groundHit = false;
        }
        
        // Landed on the Ground
        if (!_grounded && groundHit)
        {
            _grounded = true;

            _isJumping = false;
            _isDoubleJumping = false;
            _isTripleJumping = false;
            //GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
        }
        // Left the Ground
        else if (_grounded && !groundHitRay)
        {
            _grounded = false;
            _frameLeftGrounded = _time;
            //GroundedChanged?.Invoke(false, 0);
        }

        Debug.Log("Grounded: " + _grounded);
    }

    #endregion

    /*
    private void HandleHorizontalMovement()
    {
        _inputMovementVector.x = PlayerInputHandler.Instance.GetMovementInput().normalized.x;

        if (isCrouchWalking)
            _inputMovementVector.x *= crouchWalkSpeed;
        else
            _inputMovementVector.x *= walkSpeed;
    }*/
    #region Jumping

    private bool _jumpToConsume;
    private float _timeJumpWasPressed;

    private void HandleJump()
    {
        if (_jumpInputToApply && _grounded) ExecuteJump();

        else if (_jumpInputToApply && _isJumping && !_isDoubleJumping) ExecuteDoubleJump();

        else if (_jumpInputToApply && _isDoubleJumping && !_isTripleJumping) ExecuteTripleJump();

        _jumpInputToApply = false;
    }

    private void ExecuteJump()
    {
        _isJumping = true;
        _timeJumpWasPressed = 0;
        _frameVelocity.y = _stats.JumpPower;
        
        //Jumped?.Invoke();
    }
    private void ExecuteDoubleJump()
    {
        _isDoubleJumping = true;
        _frameVelocity.y = _stats.DoubleJumpPower;
        Debug.Log("Double Jumped");
    }
    private void ExecuteTripleJump()
    {
        _isTripleJumping = true;
        _frameVelocity.y = _stats.TripleJumpPower;
        Debug.Log("Triple Jumped");
    }

    #endregion

    #region Horizontal

    private void HandleHorizontalMovement()
    {
        if (_frameInput.Move.x == 0)
        {
            var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
        }
        else
        {
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
        }
    }

    #endregion

    #region Gravity

    private void HandleGravity()
    {
        if (_grounded && _frameVelocity.y <= 0f)
            _frameVelocity.y = _stats.GroundedForce;
        else
        {
            var inAirGravity = _stats.FallAcceleration;
            if ( _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
        }
    }

    #endregion

    private void ApplyMovement() => _rb.velocity = _frameVelocity;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
    }

#endif
    /*
    private void HandleSlopeMovement()
    {
        float slopeAngle = _advancedCharacterCollision2D.GetSlopeAngle();

        if (slopeAngle != 0 && IsCharacterOnTheGround())
        {
            if (_inputMovementVector.x != 0f && slopeAngle != 0f && (_inputMovementVector.x * slopeAngle > 0))
            {
                _inputMovementVector.y = -Mathf.Abs(Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * _inputMovementVector.x);
                _inputMovementVector.y *= downForceAdjustment;
            }
        }
    }*/
    private void HandleDirection()
    {
        if (_frameInput.Move.x < 0)
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        else if (_frameInput.Move.x > 0)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
    /*
    private bool IsCharacterOnTheGround()
    {
        return _advancedCharacterCollision2D.IsGrounded();
    }*/
    private void OnTheGround()
    {
        ResetVerticalMovement();

        ResetJumpStates();

        //Jump
        if (PlayerInputHandler.Instance.IsJumpButtonPressedThisFrame())
            Jump();

        //Crouch
        if (PlayerInputHandler.Instance.IsPlayerPressingDownMovementButton() && !isCrouching)
            Crouch();
        //else if (PlayerInputHandler.Instance.GetMovementInput().y >= 0 && isCrouching && HasUnCrouchSpace())
            //UnCrouch();

        //Crouch Walk
        if (isCrouching && _inputMovementVector.x != 0)
            isCrouchWalking = true;
        else
            isCrouchWalking = false;
    }
    public void ResetVerticalMovement()
    {
        _inputMovementVector.y = 0f;
    }
    private void ResetJumpStates()
    {
        //Jumps
        //isJumping = false;
        _isDoubleJumping = false;
        _isTripleJumping = false;
        isWallJumping = false;
    }
    private void Jump()
    {
        _inputMovementVector.y = jumpSpeed;
        _rb.velocity = new Vector2(_rb.velocity.x, jumpSpeed);
        //isJumping = true;
        //_advancedCharacterCollision2D.DisableGroundCheck();
    }
    private void Crouch()
    {
       // if (_advancedCharacterCollision2D.groundType == GroundType.OneWayPlatform)
         //   return;

        _col.size = new Vector2(_col.size.x, _col.size.y / 2);
        transform.position = new Vector2(transform.position.x, transform.position.y - (_originalColliderSize.y / 4));
        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp_crouching");

        isCrouching = true;
    }
    /*
    private bool HasUnCrouchSpace()
    {
        RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_col.bounds.center,
                    transform.localScale, CapsuleDirection2D.Vertical, 0f, Vector2.up,
                    _originalColliderSize.y / 2, _advancedCharacterCollision2D.layerMask);

        return !hitCeiling.collider;
    }
    private void UnCrouch()
    {
        _col.size = _originalColliderSize;

        transform.position = new Vector2(transform.position.x, transform.position.y + (_originalColliderSize.y / 4));
        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");

        isCrouching = false;
        isCrouchWalking = false;
    }
    private void OnTheAir()
    {
        if (isCrouching && _inputMovementVector.y > 0)
            StartCoroutine("ClearCrouchState");

        //if (PlayerInputHandler.Instance.JumpButtonReleased && _inputMovementVector.y > 0)
          //  _inputMovementVector.y *= 0.5f;

        if (PlayerInputHandler.Instance.IsJumpButtonPressedThisFrame())
        {
            //Triple Jump
            if (canTripleJump && !HasSideCollisions())
                if (_isDoubleJumping && !_isTripleJumping)
                    TripleJump();
            //Double Jump
            if (canDoubleJump && !HasSideCollisions())
                if (!_isDoubleJumping)
                    DoubleJump();

            //Wall Jump
            if (canWallJump && HasSideCollisions())
            {
                WallJump();

                if (autoRotatePlayerAfterWallJump)
                    HandleDirection();

                StartCoroutine("WallJumpWaiter");

                if (canJumpAfterWallJump)
                {
                    _isDoubleJumping = false;
                    _isTripleJumping = false;
                }
            }
        }
        //Wall Run
        if (canWallRun && HasSideCollisions())
        {
            if (PlayerInputHandler.Instance.GetMovementInput().y > 0 && _isJumping)
            {
                _inputMovementVector.y = wallRunAmount;

                if (_advancedCharacterCollision2D.left)
                    transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                else if (_advancedCharacterCollision2D.right)
                    transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                StartCoroutine("WallRunWaiter");
            }
        }
        else
        {
            if (canMultipleWallRun)
            {
                StopCoroutine("WallRunWaiter");
                isWallRunning = false;
            }
        }
        //Wall Slide
        if (canWallSlide && HasSideCollisions())
        {
            if (_advancedCharacterCollision2D.hitWallThisFrame)
                ResetVerticalMovement();

            if (_inputMovementVector.y <= 0)
                isWallSliding = true;
        }
        else
            isWallSliding = false;

        AdjustGravity();
    }
    private void DoubleJump()
    {
        _inputMovementVector.y = doubleJumpSpeed;
        _isDoubleJumping = true;
    }
    private void TripleJump()
    {
        _inputMovementVector.y = tripleJumpSpeed;
        _isTripleJumping = true;
    }
    private void WallJump()
    {
        if (_inputMovementVector.x <= 0 && _advancedCharacterCollision2D.left)
        {
            _inputMovementVector.x = wallJumpXSpeed;
            _inputMovementVector.y = wallJumpYSpeed;
        }
        else if (_inputMovementVector.x >= 0 && _advancedCharacterCollision2D.right)
        {
            _inputMovementVector.x = -wallJumpXSpeed;
            _inputMovementVector.y = wallJumpYSpeed;
        }

    }
    private bool HasSideCollisions()
    {
        return _advancedCharacterCollision2D.right || _advancedCharacterCollision2D.left;
    }
    void AdjustGravity()
    {
        //If Something Above Player Resets Vertical Movement
        if (_inputMovementVector.y > 0f && _advancedCharacterCollision2D.above)
            ResetVerticalMovement();

        //Wall Slide Gravity Adjustment
        if (isWallSliding)
            _inputMovementVector.y -= (gravity * wallSlideAmount) * Time.deltaTime;
        else if (!isWallSliding)
            _inputMovementVector.y -= gravity * Time.deltaTime;
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
    }
    IEnumerator ClearCrouchState()
    {
        yield return new WaitForSeconds(0.05f);
        if (HasUnCrouchSpace())
            UnCrouch();
    }
    #endregion*/
}
public struct FrameInput
{
    public bool JumpDown;
    public bool JumpHeld;
    public Vector2 Move;
}
public interface IPlayerController
{
    public event Action<bool, float> GroundedChanged;

    public event Action Jumped;
    public Vector2 FrameInput { get; }
}
