using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //player properties
    public float walkSpeed = 10f;
    public float gravity = 20f;
    public float jumpSpeed = 15f;
    public float doubleJumpSpeed = 10f;
    public float tripleJumpSpeed = 10f;
    public float wallJumpXSpeed = 15f;
    public float wallJumpYSpeed = 15f;
    public float wallRunAmount = 8f;
    
    [SerializeField] [Range(0f, 1f)] [Tooltip("Factor to multiply gravity with")]
    public float wallSlideAmount = 0.1f;

    //player ability toggles
    public bool canDoubleJump;
    public bool canTripleJump;
    public bool canWallJump;
    public bool canJumpAfterWallJump;
    public bool canWallRun;
    public bool canMultipleWallRun;
    public bool canWallSlide;

    //player state
    public bool isJumping;
    public bool isDoubleJumping;
    public bool isTripleJumping;
    public bool isWallJumping;
    public bool isWallRunning;
    public bool isWallSliding;
    public bool isDucking;
    public bool isCreeping;

    //input flags
    private bool _startJump;
    private bool _releaseJump;

    private Vector2 _input;
    private Vector2 _moveDirection;
    private CharacterController2D _characterController;

    private bool ableToWallRun = true;

    private CapsuleCollider2D _capsuleCollider;
    private Vector2 _originalColliderSize;
    //TODO: remove later when not needed
    private SpriteRenderer _spriteRenderer;

    void Start()
    {
        _characterController = gameObject.GetComponent<CharacterController2D>();
        _capsuleCollider = gameObject.GetComponent<CapsuleCollider2D>();
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        _originalColliderSize = _capsuleCollider.size;
    }
    void Update()
    {
        if(!isWallJumping)
        {
            _moveDirection.x = _input.x;
            _moveDirection.x *= walkSpeed;

            if (_moveDirection.x < 0)
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            else if (_moveDirection.x > 0)
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        //On the ground
        if (_characterController.below)
        {
            _moveDirection.y = 0f;

            //clear flags for in air abilities
            isJumping = false;
            isDoubleJumping = false;
            isTripleJumping = false;
            isWallJumping = false;

            //jumping
            if (_startJump)
            {
                _startJump = false;
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
                isCreeping = true;
            else
                isCreeping = false;
        }
        // In the air
        else
        {
            if ((isDucking || isCreeping) && _moveDirection.y > 0)
                StartCoroutine("ClearDuckingState");

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
        }
        _characterController.Move(_moveDirection * Time.deltaTime);
    }
    void GravityCalculations()
    {
        if (_moveDirection.y > 0f && _characterController.above)
        {
            _moveDirection.y = 0f;
        }
        if (canWallSlide && (_characterController.left || _characterController.right))
        {
            if(_characterController.hitWallThisFrame)
                _moveDirection.y = 0;

            if(_moveDirection.y <= 0)
                _moveDirection.y -= (gravity * wallSlideAmount) * Time.deltaTime;
            else
                _moveDirection.y -= gravity * Time.deltaTime;

        }
        else
        {
            _moveDirection.y -= gravity * Time.deltaTime;
        }

        _moveDirection.y -= gravity * Time.deltaTime;
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

    //coroutines
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
            //transform.position = new Vector2(transform.position.x, transform.position.y + (_originalColliderSize.y / 4));
            _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");
            isDucking = false;
            isCreeping = false;
        }
    }
}

