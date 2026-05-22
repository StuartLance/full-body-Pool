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

    [Tooltip("When the pull-back distance falls below this after a valid pull-back, the shot fires.")]
    [SerializeField] private float releasePullBackDistance = 0.10f;

    [Tooltip("How much tracker smoothing to apply. Higher = smoother but slightly slower.")]
    [SerializeField] private float trackerSmoothing = 12f;

    [Tooltip("Number of consecutive frames required before firing. Helps avoid noisy false triggers.")]
    [SerializeField] private int releaseConfirmationFrames = 2;

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

    private Vector3 smoothedTrackerPosition;
    private bool hasSmoothedTrackerPosition = false;

    private bool validPullBackReached = false;
    private int releaseFrameCounter = 0;

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
        Vector3 trackerPosition = GetSmoothedTrackerPosition(tracker);

        Vector3 trackerFloorPosition = trackerPosition;
        trackerFloorPosition.y = 0f;

        switch (state)
        {
            case GestureState.WaitingForAnchorDown:
                if (trackerPosition.y <= anchorDownY)
                {
                    state = GestureState.WaitingForAnchorUp;
                    Debug.Log("Anchor down detected.");
                }
                break;

            case GestureState.WaitingForAnchorUp:
                if (trackerPosition.y >= anchorUpY)
                {
                    AnchorShotAngleFromPosition(trackerPosition);
                    state = GestureState.WaitingForPullBack;
                    Debug.Log("Shot angle anchored.");
                }
                break;

            case GestureState.WaitingForPullBack:
                UpdatePullBackDistance(trackerFloorPosition);

                if (currentPullBackDistance >= minimumPullBackDistance)
                {
                    validPullBackReached = true;
                    releaseFrameCounter = 0;
                    state = GestureState.WaitingForReturn;
                    Debug.Log("Pull-back detected.");
                }
                break;

            case GestureState.WaitingForReturn:
                UpdatePullBackDistance(trackerFloorPosition);

                if (currentPullBackDistance > maxPullBackDistance)
                {
                    maxPullBackDistance = currentPullBackDistance;
                }

                bool releasedShot =
                    validPullBackReached &&
                    maxPullBackDistance >= minimumPullBackDistance &&
                    currentPullBackDistance <= releasePullBackDistance;

                if (releasedShot)
                {
                    releaseFrameCounter++;
                }
                else
                {
                    releaseFrameCounter = 0;
                }

                if (releaseFrameCounter >= releaseConfirmationFrames)
                {
                    Debug.Log(
                        $"Shot released. CurrentPullBack: {currentPullBackDistance}, MaxPullBack: {maxPullBackDistance}"
                    );

                    FireShot();
                }
                break;
        }
    }

    private Vector3 GetSmoothedTrackerPosition(Transform tracker)
    {
        if (!hasSmoothedTrackerPosition)
        {
            smoothedTrackerPosition = tracker.position;
            hasSmoothedTrackerPosition = true;
            return smoothedTrackerPosition;
        }

        smoothedTrackerPosition = Vector3.Lerp(
            smoothedTrackerPosition,
            tracker.position,
            Time.deltaTime * trackerSmoothing
        );

        return smoothedTrackerPosition;
    }

    private void AnchorShotAngleFromPosition(Vector3 trackerPosition)
    {
        anchorPosition = trackerPosition;
        anchorPosition.y = 0f;

        Vector3 ballToPlayer = anchorPosition - cueBall.position;
        ballToPlayer.y = 0f;

        if (ballToPlayer.sqrMagnitude < 0.001f)
        {
            ballToPlayer = transform.right;
        }

        anchoredBallToPlayerDirection = ballToPlayer.normalized;

        // Direction the cue ball should travel.
        shotDirection = -anchoredBallToPlayerDirection;

        currentPullBackDistance = 0f;
        maxPullBackDistance = 0f;

        validPullBackReached = false;
        releaseFrameCounter = 0;
    }

    private void UpdatePullBackDistance(Vector3 trackerFloorPosition)
    {
        Vector3 anchorToCurrent = trackerFloorPosition - anchorPosition;
        anchorToCurrent.y = 0f;

        // Positive value means the player pulled away from the cue ball.
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

        AudioManager.Instance.PlayShotSound();

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

        validPullBackReached = false;
        releaseFrameCounter = 0;
        hasSmoothedTrackerPosition = false;

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