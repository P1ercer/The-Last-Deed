
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class NPCCarControllerAI : MonoBehaviour
{
    [Header("Car Settings")]
    public float maxSpeed = 15f;
    public float acceleration = 8f;
    public float turnSpeed = 120f;
    public float waypointDistance = 4f;

    [Header("Road Settings")]
    public string roadTag = "Road";
    public bool loopRoute = true;

    private Rigidbody rb;
    private Transform[] roadPoints;
    private int currentWaypoint;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.constraints |= RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        GameObject[] roads;

        try
        {
            roads = GameObject.FindGameObjectsWithTag(roadTag);
        }
        catch (UnityException)
        {
            Debug.LogError("Create the '" + roadTag + "' tag in Unity first!");
            enabled = false;
            return;
        }

        if (roads.Length == 0)
        {
            Debug.LogError("No objects found with tag: " + roadTag);
            enabled = false;
            return;
        }

        // Sort waypoints by name: Road_001, Road_002, etc.
        roadPoints = roads
            .OrderBy(obj => obj.name)
            .Select(obj => obj.transform)
            .ToArray();

        // Find the nearest waypoint to the car.
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < roadPoints.Length; i++)
        {
            float distance = Vector3.Distance(
                transform.position,
                roadPoints[i].position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                currentWaypoint = i;
            }
        }
    }

    private void FixedUpdate()
    {
        if (roadPoints == null || roadPoints.Length == 0)
            return;

        Transform target = roadPoints[currentWaypoint];

        Vector3 direction = target.position - rb.position;
        direction.y = 0f;

        // Move to the next road waypoint.
        if (direction.magnitude < waypointDistance)
        {
            currentWaypoint++;

            if (currentWaypoint >= roadPoints.Length)
            {
                if (loopRoute)
                {
                    currentWaypoint = 0;
                }
                else
                {
                    currentSpeed = Mathf.MoveTowards(
                        currentSpeed,
                        0f,
                        acceleration * Time.fixedDeltaTime
                    );

                    DriveForward();
                    return;
                }
            }

            target = roadPoints[currentWaypoint];
            direction = target.position - rb.position;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.01f)
            return;

        // Turn toward the next waypoint.
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Quaternion rotation = Quaternion.RotateTowards(
            rb.rotation,
            targetRotation,
            turnSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(rotation);

        // Accelerate smoothly.
        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            maxSpeed,
            acceleration * Time.fixedDeltaTime
        );

        DriveForward();
    }

    private void DriveForward()
    {
        Vector3 forward = rb.rotation * Vector3.forward;

        rb.MovePosition(
            rb.position +
            forward * currentSpeed * Time.fixedDeltaTime
        );
    }

    private void OnDrawGizmosSelected()
    {
        if (roadPoints == null)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < roadPoints.Length; i++)
        {
            if (roadPoints[i] == null)
                continue;

            Gizmos.DrawSphere(roadPoints[i].position, 0.5f);

            if (i + 1 < roadPoints.Length &&
                roadPoints[i + 1] != null)
            {
                Gizmos.DrawLine(
                    roadPoints[i].position,
                    roadPoints[i + 1].position
                );
            }
        }
    }
}
