using UnityEngine;
public class CharacterController2D : MonoBehaviour
{
    //Movement
    private Vector2 _moveAmount;
    private Vector2 _currentPosition;
    private Vector2 _lastPosition;

    //Components
    private Rigidbody2D _rigidbody;
    private CapsuleCollider2D _capsuleCollider;
    
    void Start()
    {
        _rigidbody = gameObject.GetComponent<Rigidbody2D>();
        _capsuleCollider = gameObject.GetComponent<CapsuleCollider2D>();
    }
    void FixedUpdate()
    {
        _lastPosition = _rigidbody.position;

        _currentPosition = _lastPosition + _moveAmount;

        _rigidbody.MovePosition(_currentPosition);

        _moveAmount = Vector2.zero;
    }

    public void Move(Vector2 movement)
    {
        _moveAmount += movement;
    }
}
