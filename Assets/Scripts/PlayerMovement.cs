using GlobalTypes;
using System.Collections;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
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
    [SerializeField] public bool isJumping;
    [SerializeField] private bool isDoubleJumping;
    [SerializeField] private bool isTripleJumping;    
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
    private AdvancedCharacterCollision2D _advancedCharacterCollision2D;
    private CapsuleCollider2D _capsuleCollider;
    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _spriteRenderer;
    private Vector2 _originalColliderSize;

    private void Start()
    {
        _advancedCharacterCollision2D = GetComponent<AdvancedCharacterCollision2D>();
        _capsuleCollider = GetComponent<CapsuleCollider2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _originalColliderSize = _capsuleCollider.size;
    }
    private void Update()
    {
        //On the ground
        if (IsCharacterOnTheGround())
            OnTheGround();
        // In the air
        else
            OnTheAir();

        if (!isWallJumping)
        {
            HandleHorizontalMovement();
            HandleSlopeMovement();
            AdjustPlayerDirection();
        }

        transform.position += new Vector3(_inputMovementVector.x + _otherMovementVector.x, _inputMovementVector.y + _otherMovementVector.y, 0f) * Time.deltaTime;

        _otherMovementVector = Vector2.zero;
    }
    private void HandleHorizontalMovement()
    {
        _inputMovementVector.x = PlayerInputHandler.Instance.GetMovementInput().normalized.x;

        if (isCrouchWalking)
            _inputMovementVector.x *= crouchWalkSpeed;
        else
            _inputMovementVector.x *= walkSpeed;
    }
    private void HandleSlopeMovement()
    {
        float slopeAngle = _advancedCharacterCollision2D.GetSlopeAngle();

        if (slopeAngle != 0 && IsCharacterOnTheGround())
        {
            if(_inputMovementVector.x != 0f && slopeAngle != 0f && (_inputMovementVector.x * slopeAngle > 0))
            {
                _inputMovementVector.y = -Mathf.Abs(Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * _inputMovementVector.x);
                _inputMovementVector.y *= downForceAdjustment;
            }
        }
    }
    public void MoveThePlayer(Vector2 movement)
    {
        _otherMovementVector += movement;
    }
    public void ResetVerticalPhysicsMovement()
    {
        _otherMovementVector.y = 0f;
    }
    private void AdjustPlayerDirection()
    {
        if (PlayerInputHandler.Instance.GetMovementInput().x < 0)
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        else if (PlayerInputHandler.Instance.GetMovementInput().x > 0)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
    private bool IsCharacterOnTheGround()
    {
        return _advancedCharacterCollision2D.IsGrounded();
    }
    private void OnTheGround()
    {
        ResetVerticalMovement();

        ResetJumpStates();

        //Jump
        if (PlayerInputHandler.Instance.IsJumpButtonPressedThisFrame())
        {
            if (_advancedCharacterCollision2D.groundType == GroundType.MovingPlatform)
            {
                Vector2 movingPlatformVelocity = _advancedCharacterCollision2D.GetGroundCollisionObject().GetComponent<MovingPlatform>().Velocity;
                _inputMovementVector.y = jumpSpeed + movingPlatformVelocity.y;
            }
            else
                Jump();
        }

        //Crouch
        if (PlayerInputHandler.Instance.IsPlayerPressingDownMovementButton() && !isCrouching)
            Crouch();
        else if (PlayerInputHandler.Instance.GetMovementInput().y >= 0 && isCrouching && HasUnCrouchSpace())
            UnCrouch();

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
        isJumping = false;
        isDoubleJumping = false;
        isTripleJumping = false;
        isWallJumping = false;
    }
    private void Jump()
    {
        _inputMovementVector.y = jumpSpeed;
        _rigidbody2D.velocity = new Vector2(_rigidbody2D.velocity.x, jumpSpeed);
        isJumping = true;
        _advancedCharacterCollision2D.DisableGroundCheck();
    }
    private void Crouch()
    {
        if (_advancedCharacterCollision2D.groundType == GroundType.OneWayPlatform)
            return;

        _capsuleCollider.size = new Vector2(_capsuleCollider.size.x, _capsuleCollider.size.y / 2);
        transform.position = new Vector2(transform.position.x, transform.position.y - (_originalColliderSize.y / 4));
        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp_crouching");

        isCrouching = true;
    }
    private bool HasUnCrouchSpace()
    {
        RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_capsuleCollider.bounds.center,
                    transform.localScale, CapsuleDirection2D.Vertical, 0f, Vector2.up,
                    _originalColliderSize.y / 2, _advancedCharacterCollision2D.layerMask);

        return !hitCeiling.collider;
    }
    private void UnCrouch()
    {
        _capsuleCollider.size = _originalColliderSize;

        transform.position = new Vector2(transform.position.x, transform.position.y + (_originalColliderSize.y / 4));
                    _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");

        isCrouching = false;
        isCrouchWalking = false;
    }
    private void OnTheAir()
    {
        if (isCrouching && _inputMovementVector.y > 0)
            StartCoroutine("ClearCrouchState");

        if (PlayerInputHandler.Instance.JumpButtonReleased && _inputMovementVector.y > 0)
            _inputMovementVector.y *= 0.5f;

        if (PlayerInputHandler.Instance.IsJumpButtonPressedThisFrame())
        {
            //Triple Jump
            if (canTripleJump && !HasSideCollisions())
                if (isDoubleJumping && !isTripleJumping)
                    TripleJump();
            //Double Jump
            if (canDoubleJump && !HasSideCollisions())
                if (!isDoubleJumping)
                    DoubleJump();

            //Wall Jump
            if (canWallJump && HasSideCollisions())
            {
                WallJump();

                if (autoRotatePlayerAfterWallJump)
                    AdjustPlayerDirection();

                StartCoroutine("WallJumpWaiter");

                if (canJumpAfterWallJump)
                {
                    isDoubleJumping = false;
                    isTripleJumping = false;
                }
            }
        }
        //Wall Run
        if (canWallRun && HasSideCollisions())
        {
            if (PlayerInputHandler.Instance.GetMovementInput().y > 0 && isJumping)
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
        isDoubleJumping = true;
    }
    private void TripleJump()
    {
        _inputMovementVector.y = tripleJumpSpeed;
        isTripleJumping = true;
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
        else if(!isWallSliding)
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
    #endregion
}