using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NPCCarController : MonoBehaviour
{
    [Header("Car Config")]
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
        
    }
}
