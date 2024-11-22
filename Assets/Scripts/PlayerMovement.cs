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

    private bool _isFacingRight = true;

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
        //HandleSlopeMovement();
        HandleWallMovement();
        HandleCrouch();


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
        {
            _slopeAngle = Vector2.SignedAngle(groundHitRay.normal, Vector2.up);

            if (_slopeAngle > _stats.SlopeAngleLimit || _slopeAngle < -_stats.SlopeAngleLimit)
                groundHit = false;
            else
                groundHit = true;
        }
            
        else
            groundHit = false;

        //bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
        ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

        // Left and Right
        leftHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.left, _stats.GrounderDistance, ~_stats.PlayerLayer);
        rightHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.right, _stats.GrounderDistance, ~_stats.PlayerLayer);

        
        // Hit a Ceiling
        if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

        if (groundHit)
        {
            //groundType = DetectGroundType(hit.collider);
            //_groundCollisionObject = hit.collider.gameObject;

            _slopeNormal = groundHitRay.normal;
            _slopeAngle = Vector2.SignedAngle(_slopeNormal, Vector2.up);

            if (_slopeAngle > _stats.SlopeAngleLimit || _slopeAngle < -_stats.SlopeAngleLimit)
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
    
    #region Jumping

    private bool _jumpToConsume;
    private float _timeJumpWasPressed;

    private void HandleJump()
    {
        if (_jumpInputToApply && _grounded) ExecuteJump();

        else if (_jumpInputToApply && (leftHit || rightHit)) ExecuteWallJump();

        else if (_jumpInputToApply && _isJumping && !_isDoubleJumping) ExecuteDoubleJump();

        else if (_jumpInputToApply && _isDoubleJumping && !_isTripleJumping) ExecuteTripleJump();

        _jumpInputToApply = false;
    }
    private void HandleCrouch()
    {
        if (_frameInput.Move.y < 0 && groundHit && !isCrouching)
            Crouch();
        else if ((_frameInput.Move.y >= 0 || !groundHit) && !ceilingHit && isCrouching)
            UnCrouch();
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
    }
    private void ExecuteTripleJump()
    {
        _isTripleJumping = true;
        _frameVelocity.y = _stats.TripleJumpPower;
    }
    private void ExecuteWallJump()
    {
        Debug.Log("Wall Jumped !!");
        _isJumping = true;
        _timeJumpWasPressed = 0;

        if (_stats.AutoRotateAfterWallJump)
            FlipDirection();

        int direction = _isFacingRight ? 1 : -1;

        _frameVelocity.x = _stats.WallJumpHorizontalPower * direction;
        _frameVelocity.y = _stats.WallJumpVerticalPower;

        if (_stats.ResetMultipleJumpsAfterWallJump)
        {
            _isDoubleJumping = false;
            _isTripleJumping = false;
        }
    }

    #endregion

    #region Horizontal Movement

    private void HandleHorizontalMovement()
    {
        if (_frameInput.Move.x == 0)
        {
            var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
        }
        else if(isCrouching)
        {
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.CrouchSpeed, _stats.Acceleration * Time.fixedDeltaTime);
        }
        else
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
    }

    #endregion

    #region Gravity

    private void HandleGravity()
    {
        if (isWallRunning)
            return;

        if (_grounded && _frameVelocity.y <= 0f)
            _frameVelocity.y = _stats.GroundedForce;
        else
        {
            var inAirGravity = _stats.FallAcceleration;

            if((leftHit || rightHit) && _frameVelocity.y <= 0 && !_grounded) // Wall Slide
                inAirGravity *= _stats.WallSlideAmount;

            if ( _frameVelocity.y > 0) 
                inAirGravity *= _stats.JumpEndEarlyGravityModifier;

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
    
    private void HandleSlopeMovement()
    {
        if (_slopeAngle == 0 || !groundHit)
            return;

        if (_frameVelocity.x != 0f && (_frameVelocity.x * _slopeAngle > 0))
        {
            _frameVelocity.y = -Mathf.Abs(Mathf.Tan(_slopeAngle * Mathf.Deg2Rad) * _frameVelocity.x);
            _frameVelocity.y *= downForceAdjustment;
        }
    }

    private void FlipDirection()
    {
        Debug.Log("Flipped  !!");
        if(transform.rotation == Quaternion.Euler(0f, 180f, 0f))
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            _isFacingRight = true;
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            _isFacingRight = false;
        }
    }
    private void HandleDirection()
    {
        if (_frameInput.Move.x < 0)
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            _isFacingRight = false;
        }
        else if (_frameInput.Move.x > 0)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            _isFacingRight = true;
        }
            
    }
    private void OnTheGround()
    {
        ResetVerticalMovement();

        ResetJumpStates();


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
    private void Crouch()
    {
       // if (_advancedCharacterCollision2D.groundType == GroundType.OneWayPlatform)
         //   return;

        _col.size = new Vector2(_col.size.x, _col.size.y / 2);
        transform.position = new Vector2(transform.position.x, transform.position.y - (_originalColliderSize.y / 4));
        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp_crouching");

        isCrouching = true;
    }
    private bool HasUnCrouchSpace()
    {
        return !ceilingHit;
    }
    private void UnCrouch()
    {
        _col.size = _originalColliderSize;

        transform.position = new Vector2(transform.position.x, transform.position.y + (_originalColliderSize.y / 4));
        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");

        isCrouching = false;
        isCrouchWalking = false;
    }
    #region Wall Movement

    private void HandleWallMovement()
    {
        if ((leftHit || rightHit) && _frameInput.Move.y > 0)
        {
            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, _frameInput.Move.y * _stats.WallRunSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            isWallRunning = true;
        }
        else if (!groundHit || (!leftHit && !rightHit) )
            isWallRunning = false;
    }
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
#endregion