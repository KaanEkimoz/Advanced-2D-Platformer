using System.Collections;
using UnityEngine;
using GlobalTypes;
public class AdvancedCharacterCollision2D : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float raycastDistance = 0.2f;
    public LayerMask layerMask;
    public float slopeAngleLimit = 45f;
    public float downForceAdjustment = 1.2f;
    [Space]
    //Those variables only for testing purposes, can be hidden after testing
    [Header("Test Variables")]
    public bool below;
    public bool left;
    public bool right;
    public bool above;
    public GroundType groundType;

    //Movement
    private Vector2 _moveAmount;
    private Vector2 _currentPosition;
    private Vector2 _lastPosition;
    //Slope 
    private float _slopeAngle;
    private Vector2 _slopeNormal;

    //Raycast
    private Vector2[] _raycastPosition = new Vector2[3];
    private RaycastHit2D[] _raycastHits = new RaycastHit2D[3];
    
    //Flags
    [HideInInspector] public bool hitGroundThisFrame;
    [HideInInspector] public bool hitWallThisFrame;
    private bool _inAirLastFrame;
    private bool _noSideCollisionLastFrame;
    private bool _disableGroundCheck;

    //Components
    private Rigidbody2D _rigidbody;
    private CapsuleCollider2D _capsuleCollider;

    void Start()
    {
        _rigidbody = gameObject.GetComponent<Rigidbody2D>();
        _capsuleCollider = gameObject.GetComponent<CapsuleCollider2D>();
    }
    void Update()
    {
        _inAirLastFrame = !below;

        _noSideCollisionLastFrame = (!right && !left);

        _lastPosition = _rigidbody.position;
        
        if (_slopeAngle != 0 && below == true)
        {
            if((_moveAmount.x > 0f && _slopeAngle > 0f) || (_moveAmount.x < 0f && _slopeAngle < 0f))
            {
                _moveAmount.y = -Mathf.Abs(Mathf.Tan(_slopeAngle * Mathf.Deg2Rad) * _moveAmount.x);
                _moveAmount.y *= downForceAdjustment;
            }
        }
        _currentPosition = _lastPosition + _moveAmount;

        _rigidbody.MovePosition(_currentPosition);

        _moveAmount = Vector2.zero;
        
        if (!_disableGroundCheck)
            CheckGrounded();

        CheckOtherCollisions();

        if (below && _inAirLastFrame)
            hitGroundThisFrame = true;
        else
            hitGroundThisFrame = false;

        if((right || left) && _noSideCollisionLastFrame)
            hitWallThisFrame = true;
        else
            hitWallThisFrame = false;
    }
    public void Move(Vector2 movement)
    {
        _moveAmount += movement;
    }
    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, _capsuleCollider.size, CapsuleDirection2D.Vertical,
            0f, Vector2.down, raycastDistance, layerMask);

        if (hit.collider)
        {
            groundType = DetectGroundType(hit.collider);

            _slopeNormal = hit.normal;
            _slopeAngle = Vector2.SignedAngle(_slopeNormal, Vector2.up);

            if (_slopeAngle > slopeAngleLimit || _slopeAngle < -slopeAngleLimit)
                below = false;
            else
                below = true;
        }
        else
        {
            groundType = GroundType.None;
            below = false;
        }

    }
    private void CheckOtherCollisions()
    {
        //check left
        RaycastHit2D leftHit = Physics2D.BoxCast(_capsuleCollider.bounds.center, _capsuleCollider.size * 0.75f, 0f, Vector2.left,
            raycastDistance * 2f, layerMask);

        if (leftHit.collider)
            left = true;
        else
            left = false;


        //check right
        RaycastHit2D rightHit = Physics2D.BoxCast(_capsuleCollider.bounds.center, _capsuleCollider.size * 0.75f, 0f, Vector2.right,
            raycastDistance * 2f, layerMask);

        if (rightHit.collider)
            right = true;
        else
            right = false;

        //check above
        RaycastHit2D aboveHit = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, _capsuleCollider.size, CapsuleDirection2D.Vertical,
            0f, Vector2.up, raycastDistance, layerMask);

        if (aboveHit.collider)
            above = true;
        else
            above = false;
    }
    private void DrawDebugRays(Vector2 direction, Color color)
    {
        for (int i = 0; i < _raycastPosition.Length; i++)
        {
            Debug.DrawRay(_raycastPosition[i], direction * raycastDistance, color);
        }
    }
    public void DisableGroundCheck()
    {
        below = false;
        _disableGroundCheck = true;
        StartCoroutine("EnableGroundCheck");
    }
    IEnumerator EnableGroundCheck()
    {
        yield return new WaitForSeconds(0.1f);
        _disableGroundCheck = false;
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
}
