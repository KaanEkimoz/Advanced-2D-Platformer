using System.Collections;
using UnityEngine;
public class MovingPlatform : MonoBehaviour
{
    public Vector3 Velocity { get { return velocityVector; } }

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

    //Velocity
    private Vector3 velocityVector;

    //Waypoint
    private Vector3 _currentWaypoint;
    private int _currentWaypointIndex;
    private bool _canChangeCurrentWaypointIndex;
    private bool _isWaiting = false;

    private void Start()
    {
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
        Vector3 velocityVector = new Vector3(GetDirectionVector().normalized.x, GetDirectionVector().normalized.y, 0f) * movementSpeed;
        transform.position += velocityVector;
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
    private IEnumerator WaitBeforeNextWaypoint()
    {
        _isWaiting = true;
        yield return new WaitForSeconds(waitTimeBeforeChangeWaypointInSeconds);
        SetNextWaypoint();
        _isWaiting = false;
    }
}
