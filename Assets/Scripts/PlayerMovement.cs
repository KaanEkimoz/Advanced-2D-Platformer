using System;
using UnityEngine;
using GlobalTypes;
public class PlayerMovement : MonoBehaviour, IPlayerController
{
    [Header("PLAYER STATS")]
    [SerializeField] private PlayerStats _stats;

    //Flags
    private bool _isJumping;
    private bool _isDoubleJumping;
    private bool _isTripleJumping;
    private bool isWallRunning;
    private bool isWallSliding;
    private bool isGliding;
    private bool isCrouchWalking;
    private bool isCrouching;

    //Movement
    private bool _isFacingRight = true;
    private Vector2 _frameVelocity;

    //Components
    private SpriteRenderer _spriteRenderer;
    private Rigidbody2D _rigidbody2D;
    private Vector2 _originalColliderSize;
    private CapsuleCollider2D _col;

    //Events
    public event Action<bool, float> GroundedChanged;
    public event Action Jumped;

    private void Awake()
    {
        _col = GetComponent<CapsuleCollider2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
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
        HandleWallMovement();
        HandleCrouch();

        HandleGravity();

        ApplyMovement();
    }
    private void ApplyMovement() => _rigidbody2D.velocity = _frameVelocity;

    #region Input
    public Vector2 FrameInput => _frameInput.Move;
    private FrameInput _frameInput;
    private float _time;
    private void GatherInput()
    {
        _frameInput = new FrameInput
        {
            JumpDown = PlayerInputHandler.Instance.JumpButtonPressed,
            JumpHeld = PlayerInputHandler.Instance.JumpButtonHeld,
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
    #endregion

    #region Collisions

    public bool groundHit;
    public bool ceilingHit;
    public bool leftHit;
    public bool rightHit;

    private GroundType _groundType;
    private GroundType _ceilingType;

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
            {
                _groundType = DetectGroundType(groundHitRay.collider);
                groundHit = true;
            } 
        } 
        else
            groundHit = false;

        ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

        // Left and Right
        leftHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.left, _stats.GrounderDistance, ~_stats.PlayerLayer);
        rightHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.right, _stats.GrounderDistance, ~_stats.PlayerLayer);

        
        // Hit a Ceiling
        if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);
        
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


    }
    private GroundType DetectGroundType(Collider2D collider)
    {
        if (collider.GetComponent<GroundEffector>())
        {
            GroundEffector groundEffector = collider.GetComponent<GroundEffector>();
            return groundEffector.groundType;
        }
        else
            return GroundType.DefaultPlatform;
    }

    #endregion

    #region Jump

    private bool _jumpInputToApply;
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
            var deceleration = _grounded ? _stats.GroundHorizontalDeceleration : _stats.AirHorizontalDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
        }
        else if(isCrouching)
        {
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxCrouchSpeed, _stats.HorizontalAcceleration * Time.fixedDeltaTime);
        }
        else
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxHorizontalSpeed, _stats.HorizontalAcceleration * Time.fixedDeltaTime);

    }
    private void Crouch()
    {
        if (_groundType == GroundType.OneWayPlatform)
            return;

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
        if (!HasUnCrouchSpace())
            return;

        _col.size = _originalColliderSize;

        transform.position = new Vector2(transform.position.x, transform.position.y + (_originalColliderSize.y / 4));
        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");

        isCrouching = false;
        isCrouchWalking = false;
    }
    private void FlipDirection()
    {
        Debug.Log("Flipped  !!");
        if (transform.rotation == Quaternion.Euler(0f, 180f, 0f))
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

    #endregion

    #region Gravity

    private void HandleGravity()
    {
        if (isWallRunning)
            return;

        if (_grounded && _frameVelocity.y <= 0f)
        {
            _frameVelocity.y = _stats.GroundedForce;

            if (_frameVelocity.x != 0f && (_frameVelocity.x * _slopeAngle > 0))
                _frameVelocity.y = -Mathf.Abs(Mathf.Tan(_slopeAngle * Mathf.Deg2Rad) * _frameVelocity.x);

        }
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

    #region Wall Movement
    private void HandleWallMovement()
    {
        if ((leftHit || rightHit) && _frameInput.Move.y > 0)
        {
            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, _frameInput.Move.y * _stats.WallRunSpeed, _stats.HorizontalAcceleration * Time.fixedDeltaTime);
            isWallRunning = true;
        }
        else if (_frameInput.Move.y <= 0 || groundHit || (!leftHit && !rightHit) )
            isWallRunning = false;
    }
    #endregion

    #region Unity Editor Methos
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
    }
#endif
    #endregion

}

#region Helpers
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

