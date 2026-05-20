using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PoolBallPhysics : MonoBehaviour
{
    [Header("Movement Limits")]
    [SerializeField] private float stopSpeed = 0.035f;
    [SerializeField] private float stopAngularSpeed = 0.25f;

    [Header("Pool Feel")]
    [SerializeField] private float rollingResistance = 0.18f;
    [SerializeField] private float angularResistance = 0.10f;
    [SerializeField] private float maxSpeed = 18f;

    [Header("Floor Constraint")]
    [SerializeField] private bool keepFlatOnTable = true;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    private void FixedUpdate()
    {
        ApplyPoolTableResistance();
        ClampExtremeSpeed();
        StopTinyMovement();

        if (keepFlatOnTable)
        {
            RemoveUnwantedVerticalMovement();
        }
    }

    private void ApplyPoolTableResistance()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 resistanceForce = -horizontalVelocity.normalized * rollingResistance;
            rb.AddForce(resistanceForce, ForceMode.Acceleration);
        }

        if (rb.angularVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 angularResistanceForce = -rb.angularVelocity.normalized * angularResistance;
            rb.AddTorque(angularResistanceForce, ForceMode.Acceleration);
        }
    }

    private void ClampExtremeSpeed()
    {
        Vector3 velocity = rb.linearVelocity;

        Vector3 horizontalVelocity = velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }
    }

    private void StopTinyMovement()
    {
        Vector3 horizontalVelocity = rb.linearVelocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude < stopSpeed &&
            rb.angularVelocity.magnitude < stopAngularSpeed)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    private void RemoveUnwantedVerticalMovement()
    {
        if (Mathf.Abs(rb.linearVelocity.y) > 0.01f)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                Mathf.Clamp(rb.linearVelocity.y, -0.2f, 0.2f),
                rb.linearVelocity.z
            );
        }
    }
}