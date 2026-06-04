using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [Header("Defensive Effect")]
    [SerializeField] private float velocityMultiplier = 0.15f;

    [Header("Lifetime")]

    private bool hasTriggered = false;


    public void Initialize(float sizeMultiplier)
    {
        transform.localScale *= sizeMultiplier;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        Rigidbody ballRb = other.attachedRigidbody;

        if (ballRb == null)
            return;

        if (!other.CompareTag("Ball") &&
            !other.CompareTag("CueBall"))
            return;

        hasTriggered = true;

        ballRb.linearVelocity *= velocityMultiplier;
        ballRb.angularVelocity *= velocityMultiplier;

        Debug.Log($"Defender pad slowed {other.name}");

    }
}