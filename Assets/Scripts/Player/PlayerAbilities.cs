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
    public float groundSlamSpeed = 60f;
    [SerializeField] private bool isGroundSlamming;


    //Player States
    [SerializeField] private bool isPowerJumping;
    [SerializeField] private bool isDashing;


    //Glide
    private bool _startGlide = true;
    private float _currentGlideTime;

    //Timers
    private float _dashTimer;
    private float _powerJumpTimer;

    private Vector2 _targetMoveDirection;

    private void Update()
    {
        RunDashTimer();

        /*if (canPowerJump && isCrouching && _characterController.groundType != GroundType.OneWayPlatform && (_powerJumpTimer > powerJumpWaitTime))
        {
            _moveDirection.y = powerJumpSpeed;
            StartCoroutine("PowerJumpWaiter");
        }

        //canGlideAfterWallContact
        if ((_characterController.left || _characterController.right) && canWallRun)
        {
            if (canGlideAfterWallContact)
                _currentGlideTime = glideTime;
            else
                _currentGlideTime = 0;
        }*/
    }
    private void RunDashTimer()
    {
        if (_dashTimer > 0)
            _dashTimer -= Time.deltaTime;

        if (isDashing)
        {
            _targetMoveDirection.x = PlayerInputHandler.Instance.GetPlayerDirection() * dashSpeed;
            _targetMoveDirection.y = 0;
        }
    }

    #region Coroutines
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
    /* if (context.started && _dashTimer <= 0)
        {
            if ((canAirDash && !_characterController.below)
                || (canGroundDash && _characterController.below))
            {
                StartCoroutine("Dash");
            }
        }
    */
    #endregion
}
