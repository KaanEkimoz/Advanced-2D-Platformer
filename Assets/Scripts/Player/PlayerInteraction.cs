using GlobalTypes;
using System.Collections;
using UnityEngine;
public class PlayerInteraction : MonoBehaviour
{
    //Components
    private AdvancedCharacterCollision2D _characterCollision2D;
    private PlayerMovement _playerMovement;
    void Start()
    {
        _characterCollision2D = GetComponent<AdvancedCharacterCollision2D>();
        _playerMovement = GetComponent<PlayerMovement>();
    }
    void Update()
    {

        
        /*
        if(_characterCollision2D.groundType == GroundType.JumpPad)
        {
            Vector2 _jumpPadLaunchVector = _characterCollision2D.GetGroundCollisionObject().GetComponent<JumpPad>().GetLaunchVector();
            _playerMovement._movementVector += _jumpPadLaunchVector;
            //_playerMovement.isJumping = true;
        }*/

        if (_characterCollision2D.ceilingType == GroundType.OneWayPlatform && _playerMovement._inputMovementVector.y > 0f)
            StartCoroutine(DisableOneWayPlatform(_characterCollision2D.GetCeilingCollisionObject()));


        if (_characterCollision2D.groundType == GroundType.OneWayPlatform && PlayerInputHandler.Instance.IsPlayerPressingDownMovementButton())
            StartCoroutine(DisableOneWayPlatform(_characterCollision2D.GetGroundCollisionObject()));
    }
    private void FixedUpdate()
    {
        if (_characterCollision2D.groundType == GroundType.MovingPlatform)
        {
            Vector2 _currentMovingPlatformVelocity = _characterCollision2D.GetGroundCollisionObject().GetComponent<MovingPlatform>().Velocity;
            _playerMovement.PhysicsMove(_currentMovingPlatformVelocity);
        }

    }

    #region Coroutines
    IEnumerator DisableOneWayPlatform(GameObject currentOneWayPlatform)
    {
        EdgeCollider2D _oneWayPlatformEdgeCollider = currentOneWayPlatform.GetComponent<EdgeCollider2D>();

        _oneWayPlatformEdgeCollider.enabled = false;
        yield return new WaitForSeconds(0.5f);
        _oneWayPlatformEdgeCollider.enabled = true;
    }
    #endregion
}
