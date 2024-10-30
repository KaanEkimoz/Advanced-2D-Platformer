using System.Collections;
using UnityEngine;
public class PlayerMovement : MonoBehaviour
{
    [Header("Walk and Crouch Walk")]
    [SerializeField] private float walkSpeed = 10f;
    [SerializeField] private float crouchWalkSpeed = 5f;
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

    //TO DO: After fully tested the controller make the states private or protected, they are public for only testing purposes
    [Space]
    [Header("Player State")]
    [SerializeField] private bool isJumping;
    [SerializeField] private bool isDoubleJumping;
    [SerializeField] private bool isTripleJumping;    
    [SerializeField] private bool isWallJumping;
    [SerializeField] private bool isWallRunning;
    [SerializeField] private bool isWallSliding;
    [SerializeField] private bool isGliding;
    [SerializeField] private bool isCrouchWalking;
    [SerializeField] private bool isCrouching;

    //Movement
    public Vector2 _movementVector;
    private bool _isPlayerFacingRight = true;
    
    //Components
    private AdvancedCharacterCollision2D _characterController;
    private CapsuleCollider2D _capsuleCollider;
    private SpriteRenderer _spriteRenderer;
    private Vector2 _originalColliderSize;

    private void Start()
    {
        _characterController = gameObject.GetComponent<AdvancedCharacterCollision2D>();
        _capsuleCollider = gameObject.GetComponent<CapsuleCollider2D>();
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        _originalColliderSize = _capsuleCollider.size;
    }
   
    private void Update()
    {
        if (!isWallJumping)
        {
            HandleHorizontalMovement();
            AdjustPlayerDirection();
        }

        //On the ground
        if (IsCharacterOnTheGround())
            OnTheGround();
        // In the air
        else
            OnTheAir();

        _characterController.Move(_movementVector * Time.deltaTime);
    }
    private void HandleHorizontalMovement()
    {
        _movementVector.x = PlayerInputHandler.Instance.GetMovementInput().x;

        if (isCrouchWalking)
            _movementVector.x *= crouchWalkSpeed;
        else
            _movementVector.x *= walkSpeed;
    }
    private void AdjustPlayerDirection()
    {
        if (_movementVector.x < 0)
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            _isPlayerFacingRight = false;
        }
        else if (_movementVector.x > 0)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            _isPlayerFacingRight = true;
        }
    }
    private bool IsCharacterOnTheGround()
    {
        return _characterController.below;
    }
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
        else if (PlayerInputHandler.Instance.GetMovementInput().y >= 0 && isCrouching && HasUnCrouchSpace())
            UnCrouch();

        //Crouch Walk
        if (isCrouching && _movementVector.x != 0)
            isCrouchWalking = true;
        else
            isCrouchWalking = false;
    }
    private void ResetVerticalMovement()
    {
        _movementVector.y = 0f;
    }
    private void ResetJumpStates()
    {
        //jumps
        isJumping = false;
        isDoubleJumping = false;
        isTripleJumping = false;
        isWallJumping = false;
    }
    private void Jump()
    {
        _movementVector.y = jumpSpeed;
        isJumping = true;
        _characterController.DisableGroundCheck();
    }
    private void Crouch()
    {
        _capsuleCollider.size = new Vector2(_capsuleCollider.size.x, _capsuleCollider.size.y / 2);
        transform.position = new Vector2(transform.position.x, transform.position.y - (_originalColliderSize.y / 4));
        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp_crouching");

        isCrouching = true;
        
    }
    private bool HasUnCrouchSpace()
    {
        RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_capsuleCollider.bounds.center,
                    transform.localScale, CapsuleDirection2D.Vertical, 0f, Vector2.up,
                    _originalColliderSize.y / 2, _characterController.layerMask);

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
        if (isCrouching && _movementVector.y > 0)
            StartCoroutine("ClearCrouchState");

        if (PlayerInputHandler.Instance.JumpButtonReleased && _movementVector.y > 0)
            _movementVector.y *= 0.5f;

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
                _movementVector.y = wallRunAmount;

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
                isWallRunning = false;
            }
        }

        //Wall Slide
        if (canWallSlide && HasSideCollisions())
        {
            if (_characterController.hitWallThisFrame)
                ResetVerticalMovement();

            if (_movementVector.y <= 0)
                isWallSliding = true;
        }
        else
            isWallSliding = false;

        AdjustGravity();
    }
    private void DoubleJump()
    {
        _movementVector.y = doubleJumpSpeed;
        isDoubleJumping = true;
    }
    private void TripleJump()
    {
        _movementVector.y = tripleJumpSpeed;
        isTripleJumping = true;
    }
    private void WallJump()
    {
        if (_movementVector.x <= 0 && _characterController.left)
        {
            _movementVector.x = wallJumpXSpeed;
            _movementVector.y = wallJumpYSpeed;
        }
        else if (_movementVector.x >= 0 && _characterController.right)
        {
            _movementVector.x = -wallJumpXSpeed;
            _movementVector.y = wallJumpYSpeed;
        }

    }
    private bool HasSideCollisions()
    {
        return _characterController.right || _characterController.left;
    }
    void AdjustGravity()
    {
        //If Something Above Player Resets Vertical Movement
        if (_movementVector.y > 0f && _characterController.above)
            ResetVerticalMovement();

        //Wall Slide Gravity Adjustment
        if (isWallSliding)
            _movementVector.y -= (gravity * wallSlideAmount) * Time.deltaTime;
        else if(!isWallSliding)
            _movementVector.y -= gravity * Time.deltaTime;
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

