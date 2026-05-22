using UnityEngine;

public class BallAudioCollision : MonoBehaviour
{
    [SerializeField] private string ballTag = "Ball";
    [SerializeField] private string cueBallTag = "CueBall";

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsBallOrCue(collision.gameObject))
        {
            return;
        }

        // Only one of the two balls should report the collision.
        // This avoids duplicate sounds from both balls.
        if (gameObject.GetInstanceID() > collision.gameObject.GetInstanceID())
        {
            return;
        }

        float relativeVelocity = collision.relativeVelocity.magnitude;

        AudioManager.Instance.PlayBallCollisionSound(
            gameObject,
            collision.gameObject,
            relativeVelocity
        );
    }

    private bool IsBallOrCue(GameObject obj)
    {
        return obj.CompareTag(ballTag) || obj.CompareTag(cueBallTag);
    }
}