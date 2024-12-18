using System.Collections;
using UnityEngine;
using GlobalTypes;
public class AdvancedCharacterCollision2D : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float raycastDistance = 0.2f;
    public LayerMask layerMask;
    
    [Space]

    //Those variables only for testing purposes, can be made private after testing
    [Header("Test Variables")]
    public bool below;
    public bool left;
    public bool right;
    public bool above;

    public GroundType groundType;
    public GroundType ceilingType;
    public WallType rightWallType;
    public WallType leftWallType;

    private GameObject _groundCollisionObject;
    private GameObject _ceilingCollisionObject;

    //Movement
    private Vector2 _moveAmount;
    private Vector2 _currentPosition;
    private Vector2 _lastPosition;

    //Slope
    private float slopeAngleLimit = 45f;
    private float downForceAdjustment = 1.2f;
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
    public bool IsGrounded()
    {
        return below;
    }
    private void Update()
    {
        _inAirLastFrame = !below;

        _noSideCollisionLastFrame = (!right && !left);
        
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
    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, _capsuleCollider.size, CapsuleDirection2D.Vertical,
            0f, Vector2.down, raycastDistance, layerMask);

        if (hit.collider)
        {
            groundType = DetectGroundType(hit.collider);
            _groundCollisionObject = hit.collider.gameObject;

            _slopeNormal = hit.normal;
            _slopeAngle = Vector2.SignedAngle(_slopeNormal, Vector2.up);

            if (_slopeAngle > slopeAngleLimit || _slopeAngle < -slopeAngleLimit)
                below = false;
            else
                below = true;
        }
        else
        {
            _groundCollisionObject = null;
            groundType = GroundType.None;
            below = false;
        }
    }
    public float GetSlopeAngle()
    {
        return _slopeAngle;
    }
    public GameObject GetGroundCollisionObject()
    {
        return _groundCollisionObject;
    }
    public GameObject GetCeilingCollisionObject()
    {
        Vector2 raycastAbove = transform.position + new Vector3(0, _capsuleCollider.size.y * 0.5f, 0);
        RaycastHit2D hit = Physics2D.Raycast(raycastAbove, Vector2.up,
            raycastDistance, layerMask);

        Debug.DrawRay(raycastAbove, Vector2.up * raycastDistance, Color.red);
        if (hit.collider)
            return hit.collider.gameObject;

        return null;
    }
    private void CheckOtherCollisions()
    {
        //check left
        RaycastHit2D leftHit = Physics2D.BoxCast(_capsuleCollider.bounds.center, _capsuleCollider.size * 0.75f, 0f, Vector2.left,
            raycastDistance * 2f, layerMask);

        if (leftHit.collider)
        {
            left = true;
            leftWallType = DetectWallType(leftHit.collider);
        }
        else
        {
            left = false;
            leftWallType = WallType.None;
        }
            
        //check right
        RaycastHit2D rightHit = Physics2D.BoxCast(_capsuleCollider.bounds.center, _capsuleCollider.size * 0.75f, 0f, Vector2.right,
            raycastDistance * 2f, layerMask);

        if (rightHit.collider)
        {
            right = true;
            rightWallType = DetectWallType(rightHit.collider);
        }
        else
        {
            right = false;
            rightWallType = WallType.None;
        }
            
        //check above
        RaycastHit2D aboveHit = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, _capsuleCollider.size, CapsuleDirection2D.Vertical,
            0f, Vector2.up, raycastDistance, layerMask);

        if (aboveHit.collider)
        {
            ceilingType = DetectGroundType(aboveHit.collider);

            if (aboveHit.collider.gameObject.GetComponent<GroundEffector>().groundType == GroundType.OneWayPlatform)
                above = false;
            else
                above = true;
        }
        else
        {
            above = false;
            ceilingType = GroundType.None;
        }
    }
    private void DrawDebugRays(Vector2 direction, Color color)
    {
        for (int i = 0; i < _raycastPosition.Length; i++)
            Debug.DrawRay(_raycastPosition[i], direction * raycastDistance, color);
    }
    public void DisableGroundCheck()
    {
        below = false;
        _disableGroundCheck = true;
        StartCoroutine("EnableGroundCheck");
    }
    IEnumerator EnableGroundCheck()
    {
        yield return new WaitForSeconds(0.2f);
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
    private WallType DetectWallType(Collider2D collider)
    {
        if (collider.GetComponent<GroundEffector>())
        {
            WallEffector wallEffector = collider.GetComponent<WallEffector>();
            return wallEffector.wallType;
        }
        else
            return WallType.Normal;
    }
}
