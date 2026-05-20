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
    public List<Rigidbody> allBalls = new List<Rigidbody>();

    private bool localBallsMoving = false;
    private bool wasMovingLastFrame = false;
    private bool ballWasPocketedThisTurn = false;

    private bool playerWhoTookShotWasPlayer1 = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        RegisterBallsWithTag("Ball");
        RegisterBallsWithTag("CueBall");
    }

    private void Update()
    {
        CheckBallMovement();
    }

    private void RegisterBallsWithTag(string tagName)
    {
        GameObject[] ballObjects = GameObject.FindGameObjectsWithTag(tagName);

        foreach (GameObject obj in ballObjects)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();

            if (rb != null && !allBalls.Contains(rb))
            {
                allBalls.Add(rb);
            }
        }
    }

    void CheckBallMovement()
    {
        wasMovingLastFrame = localBallsMoving;
        localBallsMoving = false;

        foreach (Rigidbody rb in allBalls)
        {
            if (rb != null &&
    (rb.linearVelocity.magnitude > 0.08f || rb.angularVelocity.magnitude > 0.4f))
            {
                localBallsMoving = true;
                break;
            }
        }

        if (wasMovingLastFrame && !localBallsMoving)
        {
            OnBallsStoppedMoving();
        }
    }

    public void OnShotTaken()
    {
        ballWasPocketedThisTurn = false;

        playerWhoTookShotWasPlayer1 = isPlayer1Turn;

        SwitchTurn();
    }

    public void BallPocketed(GameObject ball)
    {
        ballWasPocketedThisTurn = true;

        if (ball.name.ToLower().Contains("cue"))
        {
            HandleScratch(ball);
            return;
        }

        if (playerWhoTookShotWasPlayer1)
        {
            player1Score++;
        }
        else
        {
            player2Score++;
        }

        Rigidbody rb = ball.GetComponent<Rigidbody>();

        if (allBalls.Contains(rb))
        {
            allBalls.Remove(rb);
        }

        Destroy(ball);
    }

    void OnBallsStoppedMoving()
    {
        Debug.Log("All balls stopped.");

        if (!ballWasPocketedThisTurn)
        {
            Debug.Log("No ball was pocketed.");
        }
        else
        {
            Debug.Log($"Player {(playerWhoTookShotWasPlayer1 ? "1" : "2")} pocketed a ball.");
        }
    }

    void HandleScratch(GameObject cueBall)
    {
        Debug.Log("Scratch!");

        RespawnCueBall(cueBall);
    }

    public void SwitchTurn()
    {
        isPlayer1Turn = !isPlayer1Turn;

        Debug.Log($"It is now Player {(isPlayer1Turn ? "1" : "2")}'s turn.");
    }

    void RespawnCueBall(GameObject cueBall)
    {
        cueBall.transform.position = new Vector3(0f, 0.5f, -2f);

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