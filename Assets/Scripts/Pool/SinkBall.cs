using UnityEngine;

public class PocketDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the pocket is actually a ball
        if (other.CompareTag("Ball") || other.CompareTag("CueBall"))
        {
            // Get the Rigidbody of the ball to stop its movement
            Rigidbody ballRb = other.GetComponent<Rigidbody>();

            if (ballRb != null)
            {
                // Zero out its velocity so it stops moving instantly
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }

            // Handle the ball entering the pocket
            ScoreBall(other.gameObject);
        }
    }

    void ScoreBall(GameObject ball)
    {
        GameManager.Instance.BallPocketed(ball);
    }

    void RespawnCueBall(GameObject cueBall)
    {
        // Move the cue ball back to the starting head string position
        // Replace Vector3.zero with your specific table's starting coordinates
        cueBall.transform.position = new Vector3(0f, 0.5f, -2f);

        Rigidbody rb = cueBall.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}