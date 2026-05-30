using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceStrength = 8f;
    [SerializeField] private float upwardLift = 0f;
    [SerializeField] private float cooldownPerBall = 0.15f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 12f;

    private readonly System.Collections.Generic.Dictionary<Rigidbody, float> lastBounceTimes =
        new System.Collections.Generic.Dictionary<Rigidbody, float>();

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void Initialize(float strength, float sizeMultiplier)
    {
        bounceStrength = strength;

        transform.localScale *= sizeMultiplier;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody ballRb = other.attachedRigidbody;

        if (ballRb == null)
            return;

        if (!other.CompareTag("Ball") && !other.CompareTag("CueBall"))
            return;

        if (lastBounceTimes.ContainsKey(ballRb))
        {
            if (Time.time - lastBounceTimes[ballRb] < cooldownPerBall)
                return;
        }

        lastBounceTimes[ballRb] = Time.time;

        Vector3 incomingVelocity = ballRb.linearVelocity;
        incomingVelocity.y = 0f;

        if (incomingVelocity.sqrMagnitude < 0.01f)
            return;

        Vector3 bounceDirection = incomingVelocity.normalized;

        Vector3 bounceImpulse =
            bounceDirection * bounceStrength +
            Vector3.up * upwardLift;

        ballRb.WakeUp();
        ballRb.AddForce(bounceImpulse, ForceMode.Impulse);

        Debug.Log($"Bounce pad hit {other.name}. Strength: {bounceStrength}");
    }
}