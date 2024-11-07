using UnityEngine;
public class MovingPlatform : MonoBehaviour
{
    public Vector2 Velocity { get { return _movementVelocity; } }

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private bool drawLineToCurrentWaypoint;

    //Movement & Velocity
    private Vector2 _movementVelocity;
    private Vector3 _lastPosition;

    //Waypoint
    private Vector3 _currentWaypoint;
    private int _currentWaypointIndex;

    private void Start()
    {
        _currentWaypointIndex = 0;
        _currentWaypoint = waypoints[_currentWaypointIndex].position;
    }
    private void Update()
    {
        UpdateLastPosition();
        MoveTowardsCurrentWaypoint();

        if(IsReachedTheWaypoint())
            SetNextWaypoint();

        CalculateVelocity();
    }
    private void UpdateLastPosition()
    {
        _lastPosition = transform.position;
    }
    private void MoveTowardsCurrentWaypoint()
    {
        transform.position = Vector3.MoveTowards(transform.position, _currentWaypoint, movementSpeed * Time.deltaTime);
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
    }
    private void CalculateVelocity()
    {
        _movementVelocity = transform.position - _lastPosition;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, _currentWaypoint);
    }
}
