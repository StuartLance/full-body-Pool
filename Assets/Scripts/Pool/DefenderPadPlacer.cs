using UnityEngine;

public class DefenderPadPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player1Tracker;
    [SerializeField] private Transform player2Tracker;
    [SerializeField] private GameObject bouncePadPrefab;

    [Header("Placement")]
    [SerializeField] private float placeHeightThreshold = 0.25f;
    [SerializeField] private float placementY = 0.06f;

    [Header("Timing")]
    [SerializeField] private float maxDecisionTime = 3f;

    [Header("Pad Size")]
    [SerializeField] private float maxPadSize = 2.2f;
    [SerializeField] private float minPadSize = 0.4f;

    [Header("Gesture Safety")]
    [SerializeField] private float rearmHeight = 0.55f;
    [SerializeField] private float placementCooldown = 0.4f;

    private bool padUsedThisTurn = false;
    private bool gestureArmed = true;

    private float turnStartTime;
    private float lastPlacementTime;

    private int lastProcessedTurn = -1;

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GamePhase.NormalPlay)
            return;

        if (GameManager.Instance.TurnNumber != lastProcessedTurn)
        {
            lastProcessedTurn = GameManager.Instance.TurnNumber;

            padUsedThisTurn = false;
            gestureArmed = true;
            turnStartTime = Time.time;

            Debug.Log(
                $"Defender reset. Attacker is now Player " +
                $"{(GameManager.Instance.isPlayer1Turn ? "1" : "2")}"
            );
        }

        if (GameManager.Instance.AreBallsMoving())
            return;

        if (padUsedThisTurn)
            return;

        float elapsed = Time.time - turnStartTime;

        // Defender waited too long
        if (elapsed > maxDecisionTime)
        {
            padUsedThisTurn = true;

            Debug.Log("Defender missed placement window.");
            return;
        }

        Transform defenderTracker = GetDefenderTracker();

        if (defenderTracker == null)
            return;

        UpdateGestureArming(defenderTracker);

        if (!gestureArmed)
            return;

        if (Time.time - lastPlacementTime < placementCooldown)
            return;

        if (defenderTracker.position.y <= placeHeightThreshold)
        {
            TryPlacePad(defenderTracker.position);
        }
    }

    private void UpdateGestureArming(Transform defenderTracker)
    {
        if (defenderTracker.position.y >= rearmHeight)
        {
            gestureArmed = true;
        }
    }

    private Transform GetDefenderTracker()
    {
        bool player1IsAttacker = GameManager.Instance.isPlayer1Turn;

        return player1IsAttacker
            ? player2Tracker
            : player1Tracker;
    }

    private void TryPlacePad(Vector3 trackerPosition)
    {
        Vector3 placementPosition = trackerPosition;
        placementPosition.y = placementY;

        float elapsed = Time.time - turnStartTime;

        float sizeT =
            Mathf.Clamp01(elapsed / maxDecisionTime);

        float padSize =
            Mathf.Lerp(maxPadSize, minPadSize, sizeT);

        SpawnPad(
            placementPosition,
            padSize
        );

        padUsedThisTurn = true;
        gestureArmed = false;
        lastPlacementTime = Time.time;

        Debug.Log(
            $"Pad placed. Size={padSize:F2} Time={elapsed:F2}s"
        );
    }

    private void SpawnPad(
        Vector3 position,
        float size)
    {
        if (bouncePadPrefab == null)
        {
            Debug.LogError("Bounce Pad Prefab missing.");
            return;
        }

        GameObject padObject =
            Instantiate(
                bouncePadPrefab,
                position,
                Quaternion.identity
            );

        BouncePad bouncePad =
            padObject.GetComponent<BouncePad>();

        if (bouncePad != null)
        {
            bouncePad.Initialize(size);
        }
    }
}