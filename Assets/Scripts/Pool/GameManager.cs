using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int player1Score = 0;
    public int player2Score = 0;
    public bool isPlayer1Turn = true;

    [Header("Ball Tracking")]
    // This list will hold all active balls on the table
    public List<Rigidbody> allBalls = new List<Rigidbody>();

    private bool localBallsMoving = false;
    private bool wasMovingLastFrame = false;
    private bool ballWasPocketedThisTurn = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Automatically find all objects tagged "Ball" and grab their Rigidbodies
        GameObject[] ballObjects = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject obj in ballObjects)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null) allBalls.Add(rb);
        }
    }

    private void Update()
    {
        CheckBallMovement();
    }

    void CheckBallMovement()
    {
        wasMovingLastFrame = localBallsMoving;
        localBallsMoving = false;

        // Loop through all remaining balls to see if ANY of them are still rolling
        foreach (Rigidbody rb in allBalls)
        {
            if (rb != null && rb.linearVelocity.magnitude > 0.05f)
            {
                localBallsMoving = true;
                break; // At least one ball is moving, no need to check the rest
            }
        }

        // DELTA DETECTOR: The exact moment ALL balls have finally come to a stop
        if (wasMovingLastFrame && !localBallsMoving)
        {
            OnBallsStoppedMoving();
        }
    }

    // This public method lets your PoolStick tell the manager a shot was taken
    public void OnShotTaken()
    {
        ballWasPocketedThisTurn = false;
    }

    public void BallPocketed(GameObject ball)
    {
        ballWasPocketedThisTurn = true;

        if (ball.name.ToLower().Contains("cue"))
        {
            HandleScratch(ball);
            return;
        }

        if (isPlayer1Turn) player1Score++;
        else player2Score++;

        // Remove the ball from our tracking list before destroying it
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (allBalls.Contains(rb)) allBalls.Remove(rb);

        Destroy(ball);
    }

    void OnBallsStoppedMoving()
    {
        Debug.Log("All balls stopped. Evaluating turn...");

        // Rule: If you didn't pocket a ball, you lose your turn.
        if (!ballWasPocketedThisTurn)
        {
            SwitchTurn();
        }
        else
        {
            Debug.Log($"Player {(isPlayer1Turn ? "1" : "2")} pocketed a ball! They shoot again.");
        }
    }

    void HandleScratch(GameObject cueBall)
    {
        Debug.Log("Scratch!");
        SwitchTurn();
        RespawnCueBall(cueBall);
    }

    public void SwitchTurn()
    {
        isPlayer1Turn = !isPlayer1Turn;
        Debug.Log($"It is now Player {(isPlayer1Turn ? "1" : "2")}'s turn.");
    }

    void RespawnCueBall(GameObject cueBall)
    {
        cueBall.transform.position = new Vector3(0f, 0.5f, -2f); // Adjust to your table setup
        Rigidbody rb = cueBall.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public bool AreBallsMoving()
    {
        return localBallsMoving;
    }
}