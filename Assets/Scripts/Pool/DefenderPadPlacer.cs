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
    [SerializeField] private LayerMask tableBoundsMask;

    [Header("Pad Decay")]
    [SerializeField] private float fullPowerTimeWindow = 1.5f;
    [SerializeField] private float maxDecisionTime = 8f;

    [SerializeField] private float maxPadStrength = 8f;
    [SerializeField] private float minPadStrength = 2f;

    [SerializeField] private float maxPadSize = 1.3f;
    [SerializeField] private float minPadSize = 0.45f;

    [Header("Gesture Safety")]
    [SerializeField] private float rearmHeight = 0.55f;
    [SerializeField] private float placementCooldown = 0.4f;

    private bool padUsedThisTurn = false;
    private bool gestureArmed = true;
    private float turnStartTime;
    private float lastPlacementTime;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnStarted += HandleTurnStarted;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTurnStarted -= HandleTurnStarted;
        }
    }

    private void Start()
    {
        HandleTurnStarted();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.NormalPlay)
        {
            return;
        }
        if (GameManager.Instance == null)
            return;                     

        if (GameManager.Instance.AreBallsMoving())
            return;

        if (padUsedThisTurn)
            return;

        Transform defenderTracker = GetDefenderTracker();
        Debug.Log(
            $"Attacker={(GameManager.Instance.isPlayer1Turn ? "P1" : "P2")} " +
            $"Defender={defenderTracker.name} " +
            $"Height={defenderTracker.position.y} " +
            $"PadUsed={padUsedThisTurn} " +
            $"GestureArmed={gestureArmed}"
        ); 
        
        if (GameManager.Instance.CurrentPhase != GamePhase.NormalPlay)
        {
            return;
        }               

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

    private void HandleTurnStarted()
{
    padUsedThisTurn = false;

    // Allow placement immediately every turn.
    gestureArmed = true;

    turnStartTime = Time.time;

    Debug.Log(
        $"Defender reset. Attacker is now Player {(GameManager.Instance.isPlayer1Turn ? "1" : "2")}"
    );
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

        return player1IsAttacker ? player2Tracker : player1Tracker;
    }

    private void TryPlacePad(Vector3 trackerPosition)
    {
        Vector3 placementPosition = trackerPosition;
        placementPosition.y = placementY;

        float elapsed = Time.time - turnStartTime;

        float decayT;

        if (elapsed <= fullPowerTimeWindow)
        {
            decayT = 0f;
        }
        else
        {
            decayT = Mathf.InverseLerp(
                fullPowerTimeWindow,
                maxDecisionTime,
                elapsed
            );
        }

        float padStrength = Mathf.Lerp(maxPadStrength, minPadStrength, decayT);
        float padSize = Mathf.Lerp(maxPadSize, minPadSize, decayT);

        SpawnPad(placementPosition, padStrength, padSize);

        padUsedThisTurn = true;
        gestureArmed = false;
        lastPlacementTime = Time.time;

        Debug.Log($"Bounce pad placed. Strength: {padStrength}, Size: {padSize}, Time: {elapsed}");
    }

    private void SpawnPad(Vector3 position, float strength, float size)
    {
        if (bouncePadPrefab == null)
        {
            Debug.LogError("Bounce pad prefab is not assigned.");
            return;
        }

        GameObject padObject = Instantiate(
            bouncePadPrefab,
            position,
            Quaternion.identity
        );

        BouncePad bouncePad = padObject.GetComponent<BouncePad>();

        if (bouncePad != null)
        {
            bouncePad.Initialize(strength, size);
        }
    }
}