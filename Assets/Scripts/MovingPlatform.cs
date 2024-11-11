using System.Collections;
using UnityEngine;
public class MovingPlatform : MonoBehaviour
{
    public Vector2 Velocity { get { return _platformRigidbody2D.velocity; } }

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [Space]
    [Header("Waypoint Settings")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waitTimeBeforeChangeWaypointInSeconds = 0.3f;
    [Space]
    [Header("Components")]
    [SerializeField] private BoxCollider2D triggerCollider;
    [Header("Test & Debug")]
    [SerializeField] private bool drawLineToCurrentWaypoint = true;

    //Waypoint
    private Vector3 _currentWaypoint;
    private int _currentWaypointIndex;
    private bool _canChangeCurrentWaypointIndex;
    private bool _isWaiting = false;

    //Components
    private Rigidbody2D _platformRigidbody2D;

    private void Start()
    {
        _platformRigidbody2D = GetComponent<Rigidbody2D>();

        _currentWaypointIndex = 0;
        _currentWaypoint = waypoints[_currentWaypointIndex].position;
        _canChangeCurrentWaypointIndex = true;
    }
    private void Update()
    {
        if (IsReachedTheWaypoint() && _canChangeCurrentWaypointIndex)
        {
            _canChangeCurrentWaypointIndex = false;
            StartCoroutine(WaitBeforeNextWaypoint());
        }
    }
    private void FixedUpdate()
    {
        MoveTowardsCurrentWaypoint();
    }
    private Vector2 GetDirectionVector()
    {
        if (_isWaiting)
            return Vector2.zero;
        return _currentWaypoint - transform.position;
    }
    private void MoveTowardsCurrentWaypoint()
    {
        _platformRigidbody2D.velocity = GetDirectionVector().normalized * movementSpeed;
    }
    private bool IsReachedTheWaypoint()
    {
        return Vector3.Distance(transform.position, _currentWaypoint) < 0.05f;
    }
    private void IncrementWaypointIndex()
    {
        _currentWaypointIndex++;

        if (_currentWaypointIndex >= waypoints.Length)
            _currentWaypointIndex = 0;
    }
    private void SetNextWaypoint()
    {
        IncrementWaypointIndex();
        _currentWaypoint = waypoints[_currentWaypointIndex].position;
        _canChangeCurrentWaypointIndex = true;
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, _currentWaypoint);
    }
    private IEnumerator WaitBeforeNextWaypoint()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(waitTimeBeforeChangeWaypointInSeconds);
        SetNextWaypoint();
        _isWaiting = false;
    }
}
