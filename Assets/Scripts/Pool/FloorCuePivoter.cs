using UnityEngine;
using System.Collections;

public class FloorCueGesturePivoter : MonoBehaviour
{
    private enum GestureState
    {
        WaitingForAnchorDown,
        WaitingForAnchorUp,
        WaitingForPullBack,
        WaitingForReturn,
        ShootingDisabled
    }

    [Header("References")]
    [SerializeField] private Transform cueBall;
    [SerializeField] private Rigidbody cueBallRigidbody;

    [Header("Players")]
    [SerializeField] private Transform player1Tracker;
    [SerializeField] private Transform player2Tracker;

    [Header("Cue Visual")]
    [SerializeField] private Transform cueStickVisual;

    [Header("Floor Alignment")]
    [SerializeField] private float heightOffset = 0.02f;
    [SerializeField] private float positionSmoothness = 25f;
    [SerializeField] private float rotationSmoothness = 25f;

    [Header("Cue Axis")]
    [SerializeField] private Vector3 cueLocalAxis = Vector3.right;

    [Header("Anchor Gesture")]
    [SerializeField] private float anchorDownY = 0.25f;
    [SerializeField] private float anchorUpY = 0.55f;

    [Header("Pull Back Gesture")]
    [SerializeField] private float minimumPullBackDistance = 0.25f;
    [SerializeField] private float maximumPullBackDistance = 2.0f;
    [SerializeField] private float returnToAnchorDistance = 0.20f;
    [SerializeField] private float returnSpeedThreshold = 0.6f;

    [Header("Shot Force")]
    [SerializeField] private float forcePerMeter = 22f;
    [SerializeField] private float minimumShotForce = 4f;
    [SerializeField] private float maximumShotForce = 45f;

    [Header("Cue Animation")]
    [SerializeField] private float visualPullBackScale = 2.5f;
    [SerializeField] private float slowShotDuration = 0.25f;
    [SerializeField] private float fastShotDuration = 0.07f;
    [SerializeField] private float afterShotHideDelay = 0.05f;

    private GestureState state = GestureState.WaitingForAnchorDown;

    private Vector3 anchorPosition;
    private Vector3 anchoredBallToPlayerDirection;
    private Vector3 shotDirection;

    private Vector3 lastTrackerPosition;
    private Vector3 trackerVelocity;

    private Vector3 cueBaseLocalPosition;

    private float currentPullBackDistance;
    private float maxPullBackDistance;

    private bool isBallMoving;
    private bool cueVisible = true;
    private bool shotAnimationPlaying;

    private void Start()
    {
        if (cueStickVisual != null)
        {
            cueBaseLocalPosition = cueStickVisual.localPosition;
        }

        Transform currentTracker = GetCurrentPlayerTracker();

        if (currentTracker != null)
        {
            lastTrackerPosition = currentTracker.position;
        }

        SetStickVisibility(true);
    }

    private void Update()
    {
        if (GameManager.Instance != null)
        {
            isBallMoving = GameManager.Instance.AreBallsMoving();
        }

        if (isBallMoving)
        {
            ResetGesture();
            SetStickVisibility(false);
            return;
        }

        if (!shotAnimationPlaying)
        {
            SetStickVisibility(true);
        }

        Transform currentTracker = GetCurrentPlayerTracker();

        if (currentTracker == null || cueBall == null)
            return;

        CalculateTrackerVelocity(currentTracker);
        UpdateGesture(currentTracker);
        UpdateCuePullBackVisual();
    }

    private void LateUpdate()
    {
        if (cueBall == null)
            return;

        if (isBallMoving || shotAnimationPlaying)
            return;

        Transform currentTracker = GetCurrentPlayerTracker();

        if (currentTracker == null)
            return;

        Vector3 targetPosition = cueBall.position + Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * positionSmoothness
        );

        Vector3 ballToPlayer;

        if (state == GestureState.WaitingForPullBack || state == GestureState.WaitingForReturn)
        {
            ballToPlayer = anchoredBallToPlayerDirection;
        }
        else
        {
            ballToPlayer = currentTracker.position - cueBall.position;
            ballToPlayer.y = 0f;

            if (ballToPlayer.sqrMagnitude < 0.001f)
                return;

            ballToPlayer.Normalize();
        }

        Quaternion targetRotation = Quaternion.FromToRotation(cueLocalAxis, ballToPlayer);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotationSmoothness
        );
    }

    private void UpdateGesture(Transform tracker)
    {
        Vector3 trackerFloorPosition = tracker.position;
        trackerFloorPosition.y = 0f;

        switch (state)
        {
            case GestureState.WaitingForAnchorDown:
                if (tracker.position.y <= anchorDownY)
                {
                    state = GestureState.WaitingForAnchorUp;
                    Debug.Log("Anchor down detected.");
                }
                break;

            case GestureState.WaitingForAnchorUp:
                if (tracker.position.y >= anchorUpY)
                {
                    AnchorShotAngle(tracker);
                    state = GestureState.WaitingForPullBack;
                    Debug.Log("Shot angle anchored.");
                }
                break;

            case GestureState.WaitingForPullBack:
                UpdatePullBackDistance(trackerFloorPosition);

                if (currentPullBackDistance >= minimumPullBackDistance)
                {
                    state = GestureState.WaitingForReturn;
                    Debug.Log("Pull-back detected.");
                }
                break;

            case GestureState.WaitingForReturn:
                UpdatePullBackDistance(trackerFloorPosition);

                float distanceToAnchor = Vector3.Distance(trackerFloorPosition, anchorPosition);
                float forwardReturnSpeed = Vector3.Dot(trackerVelocity, shotDirection);

                if (distanceToAnchor <= returnToAnchorDistance &&
                    maxPullBackDistance >= minimumPullBackDistance &&
                    forwardReturnSpeed >= returnSpeedThreshold)
                {
                    FireShot();
                }
                break;
        }
    }

    private void AnchorShotAngle(Transform tracker)
    {
        anchorPosition = tracker.position;
        anchorPosition.y = 0f;

        Vector3 ballToPlayer = anchorPosition - cueBall.position;
        ballToPlayer.y = 0f;

        if (ballToPlayer.sqrMagnitude < 0.001f)
        {
            ballToPlayer = transform.right;
        }

        anchoredBallToPlayerDirection = ballToPlayer.normalized;

        // Shot direction is from the player side into the cue ball.
        shotDirection = -anchoredBallToPlayerDirection;

        currentPullBackDistance = 0f;
        maxPullBackDistance = 0f;
    }

    private void UpdatePullBackDistance(Vector3 trackerFloorPosition)
    {
        Vector3 anchorToCurrent = trackerFloorPosition - anchorPosition;
        anchorToCurrent.y = 0f;

        // Pulling back means moving away from the cue ball, toward the player side.
        float pullDistance = Vector3.Dot(anchorToCurrent, anchoredBallToPlayerDirection);

        currentPullBackDistance = Mathf.Clamp(
            pullDistance,
            0f,
            maximumPullBackDistance
        );

        if (currentPullBackDistance > maxPullBackDistance)
        {
            maxPullBackDistance = currentPullBackDistance;
        }
    }

    private void FireShot()
    {
        if (cueBallRigidbody == null)
            return;

        float shotForce = maxPullBackDistance * forcePerMeter;
        shotForce = Mathf.Clamp(shotForce, minimumShotForce, maximumShotForce);

        Debug.Log($"FireShot called. PullBack: {maxPullBackDistance}, ShotForce: {shotForce}, ShotDirection: {shotDirection}");

        StartCoroutine(PlayCueStrikeAnimationAndShoot(shotForce));
    }

    private IEnumerator PlayCueStrikeAnimationAndShoot(float shotForce)
    {
        shotAnimationPlaying = true;
        state = GestureState.ShootingDisabled;

        float normalizedPower = Mathf.InverseLerp(
            minimumShotForce,
            maximumShotForce,
            shotForce
        );

        float shotDuration = Mathf.Lerp(
            slowShotDuration,
            fastShotDuration,
            normalizedPower
        );

        Vector3 pulledBackLocalPosition =
            cueBaseLocalPosition + Vector3.right * (maxPullBackDistance * visualPullBackScale);

        Vector3 hitLocalPosition = cueBaseLocalPosition;

        if (cueStickVisual != null)
        {
            cueStickVisual.localPosition = pulledBackLocalPosition;
        }

        float timer = 0f;

        while (timer < shotDuration)
        {
            timer += Time.deltaTime;
            float t = timer / shotDuration;

            if (cueStickVisual != null)
            {
                cueStickVisual.localPosition = Vector3.Lerp(
                    pulledBackLocalPosition,
                    hitLocalPosition,
                    t
                );
            }

            yield return null;
        }

        if (cueStickVisual != null)
        {
            cueStickVisual.localPosition = cueBaseLocalPosition;
        }

        ApplyRealisticCueStrike(shotForce);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnShotTaken();
        }

        yield return new WaitForSeconds(afterShotHideDelay);

        SetStickVisibility(false);

        shotAnimationPlaying = false;
        ResetGesture();
    }

    private void UpdateCuePullBackVisual()
    {
        if (cueStickVisual == null)
            return;

        if (state != GestureState.WaitingForPullBack && state != GestureState.WaitingForReturn)
        {
            cueStickVisual.localPosition = Vector3.Lerp(
                cueStickVisual.localPosition,
                cueBaseLocalPosition,
                Time.deltaTime * 12f
            );

            return;
        }

        Vector3 targetLocalPosition =
            cueBaseLocalPosition + Vector3.right * (currentPullBackDistance * visualPullBackScale);

        cueStickVisual.localPosition = Vector3.Lerp(
            cueStickVisual.localPosition,
            targetLocalPosition,
            Time.deltaTime * 15f
        );
    }

    private void CalculateTrackerVelocity(Transform tracker)
    {
        Vector3 distanceMoved = tracker.position - lastTrackerPosition;
        distanceMoved.y = 0f;

        trackerVelocity = distanceMoved / Time.deltaTime;
        lastTrackerPosition = tracker.position;
    }

    private Transform GetCurrentPlayerTracker()
    {
        if (GameManager.Instance == null)
            return player1Tracker;

        return GameManager.Instance.isPlayer1Turn ? player1Tracker : player2Tracker;
    }

    private void ResetGesture()
    {
        state = GestureState.WaitingForAnchorDown;

        currentPullBackDistance = 0f;
        maxPullBackDistance = 0f;

        if (cueStickVisual != null)
        {
            cueStickVisual.localPosition = cueBaseLocalPosition;
        }
    }

    private void SetStickVisibility(bool visible)
    {
        if (cueVisible == visible)
            return;

        cueVisible = visible;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer r in renderers)
        {
            r.enabled = visible;
        }
    }

    private void ApplyRealisticCueStrike(float shotForce)
    {
        if (cueBallRigidbody == null)
        {
            Debug.LogError("Cue ball Rigidbody is not assigned.");
            return;
        }

        if (cueBall == null)
        {
            Debug.LogError("Cue ball Transform is not assigned.");
            return;
        }

        Vector3 cleanShotDirection = shotDirection;
        cleanShotDirection.y = 0f;

        if (cleanShotDirection.sqrMagnitude < 0.001f)
        {
            Debug.LogError("Shot direction is zero. Cannot shoot.");
            return;
        }

        cleanShotDirection.Normalize();

        shotForce = Mathf.Clamp(shotForce, minimumShotForce, maximumShotForce);

        float normalizedPower = Mathf.InverseLerp(
            minimumShotForce,
            maximumShotForce,
            shotForce
        );

        // These are gameplay speeds. Tune these, not the force, if the shot feels wrong.
        float minBallSpeed = 3f;
        float maxBallSpeed = 14f;

        float shotSpeed = Mathf.Lerp(minBallSpeed, maxBallSpeed, normalizedPower);

        Debug.Log($"SHOOTING cue ball. Direction: {cleanShotDirection}, Force: {shotForce}, Speed: {shotSpeed}");

        cueBallRigidbody.WakeUp();

        // Clear old movement.
        cueBallRigidbody.linearVelocity = Vector3.zero;
        cueBallRigidbody.angularVelocity = Vector3.zero;

        // Directly set the pool ball velocity.
        cueBallRigidbody.linearVelocity = cleanShotDirection * shotSpeed;

        // Add realistic rolling spin.
        SphereCollider sphereCollider = cueBall.GetComponent<SphereCollider>();

        float ballRadius = 0.5f;

        if (sphereCollider != null)
        {
            ballRadius = sphereCollider.radius * cueBall.lossyScale.x;
        }

        Vector3 rollAxis = Vector3.Cross(Vector3.up, cleanShotDirection).normalized;

        // For rolling, angular speed is roughly linear speed / radius.
        cueBallRigidbody.angularVelocity = rollAxis * (shotSpeed / ballRadius);

        Debug.Log($"Cue ball velocity after direct set: {cueBallRigidbody.linearVelocity}");
    }
}