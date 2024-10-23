using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //player properties
    public float walkSpeed = 10f;
    public float gravity = 20f;
    public float jumpSpeed = 15f;
    public float doubleJumpSpeed;
    public float tripleJumpSpeed;
    public float wallJumpXSpeed;
    public float wallJumpYSpeed;
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

    //input flags
    private bool _startJump;
    private bool _releaseJump;

    private Vector2 _input;
    private Vector2 _moveDirection;
    private CharacterController2D _characterController;

    private bool ableToWallRun = true;

    void Start()
    {
        _characterController = gameObject.GetComponent<CharacterController2D>();
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

            if (_startJump)
            {
                _startJump = false;
                _moveDirection.y = jumpSpeed;
                isJumping = true;
                _characterController.DisableGroundCheck();
                ableToWallRun = true;
            }
        }
        else // In the air
        {
            if (_releaseJump)
            {
                _releaseJump = false;

                if (_moveDirection.y > 0)
                    _moveDirection.y *= 0.5f;
            }

            // double and triple jump
            if (_startJump)
            {
                if (canTripleJump && !_characterController.left && !_characterController.right)
                {
                    if (isDoubleJumping && !isTripleJumping)
                    {
                        _moveDirection.y = tripleJumpSpeed;
                        isTripleJumping = true;
                    }
                }
                if (canDoubleJump && !_characterController.left && !_characterController.right)
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

                    //isWallJumping = true;

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
                    {
                        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                    else if (_characterController.right)
                    {
                        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    }

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
                _moveDirection.y -= gravity * wallSlideAmount * Time.deltaTime;
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
}

