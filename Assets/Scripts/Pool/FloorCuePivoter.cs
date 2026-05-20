using UnityEngine;

public class FloorCuePivoter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cueBall;
    [SerializeField] private Transform playerTracker;
    [SerializeField] private Rigidbody cueBallRigidbody;

    [Header("Floor Alignment")]
    [SerializeField] private float heightOffset = 0.02f;
    [SerializeField] private float positionSmoothness = 25f;
    [SerializeField] private float rotationSmoothness = 25f;

    [Header("Physical Striking")]
    [SerializeField] private float strikeVelocityThreshold = 1.8f;
    [SerializeField] private float forceMultiplier = 12f;
    [SerializeField] private float maxForce = 45f;

    [Header("Cue Axis")]
    [Tooltip("Use +X because your CueStick child is positioned at local X = 20.")]
    [SerializeField] private Vector3 cueLocalAxis = Vector3.right;

    private Vector3 lastSensorPosition;
    private Vector3 sensorVelocity;
    private bool isBallMoving = false;

    private void Start()
    {
        if (playerTracker != null)
        {
            lastSensorPosition = playerTracker.position;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null)
        {
            isBallMoving = GameManager.Instance.AreBallsMoving();
        }

        SetStickVisibility(!isBallMoving);

        if (isBallMoving) return;

        CalculateSensorVelocity();
        CheckForPhysicalStrike();
    }

    private void LateUpdate()
    {
        if (isBallMoving || cueBall == null || playerTracker == null) return;

        // Keep the pivot exactly on the cue ball.
        Vector3 targetPosition = cueBall.position + Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * positionSmoothness
        );

        // Direction from cue ball to player on the floor plane.
        Vector3 ballToPlayer = playerTracker.position - cueBall.position;
        ballToPlayer.y = 0f;

        if (ballToPlayer.sqrMagnitude < 0.001f)
            return;

        Vector3 targetDirection = ballToPlayer.normalized;

        // IMPORTANT:
        // Your visible cue stick is offset along the pivot's local +X axis.
        // So we rotate the pivot so that local +X points toward the player.
        Quaternion targetRotation = Quaternion.FromToRotation(cueLocalAxis, targetDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSmoothness
        );
    }

    private void CalculateSensorVelocity()
    {
        if (playerTracker == null) return;

        Vector3 distanceMoved = playerTracker.position - lastSensorPosition;
        distanceMoved.y = 0f;

        sensorVelocity = distanceMoved / Time.deltaTime;
        lastSensorPosition = playerTracker.position;
    }

    private void CheckForPhysicalStrike()
    {
        if (playerTracker == null || cueBall == null || cueBallRigidbody == null) return;

        Vector3 directionToBall = cueBall.position - playerTracker.position;
        directionToBall.y = 0f;

        if (directionToBall.sqrMagnitude < 0.001f)
            return;

        directionToBall.Normalize();

        float forwardSpeed = Vector3.Dot(sensorVelocity, directionToBall);

        if (forwardSpeed > strikeVelocityThreshold)
        {
            ExecutePhysicalShot(directionToBall, forwardSpeed);
        }
    }

    private void ExecutePhysicalShot(Vector3 shotDirection, float physicalSpeed)
    {
        float calculatedForce = physicalSpeed * forceMultiplier;
        calculatedForce = Mathf.Clamp(calculatedForce, 0f, maxForce);

        cueBallRigidbody.AddForce(shotDirection * calculatedForce, ForceMode.Impulse);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShotTaken();
        }
    }

    private void SetStickVisibility(bool visible)
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}