using UnityEngine;
using System.Collections.Generic;
using System;

public enum GamePhase
{
    WaitingForPlayers,
    BreakShot,
    NormalPlay,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int player1Score = 0;
    public int player2Score = 0;

    public bool isPlayer1Turn = true;

    [Header("Win Condition")]
    [SerializeField] private int scoreToWin = 8;

    public int WinningPlayer { get; private set; } = 0;

    [Header("Ball Tracking")]
    public List<Rigidbody> allBalls = new List<Rigidbody>();

    public event Action OnTurnStarted;

    public int TurnNumber { get; private set; } = 0;

    public GamePhase CurrentPhase { get; private set; }

    private bool localBallsMoving = false;
    private bool wasMovingLastFrame = false;
    private bool ballWasPocketedThisTurn = false;

    private bool playerWhoTookShotWasPlayer1 = true;
    private bool breakShotTaken = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        CurrentPhase = GamePhase.WaitingForPlayers;

        RegisterBallsWithTag("Ball");
        RegisterBallsWithTag("CueBall");
    }

    private void Update()
    {
        CheckBallMovement();
    }

    public void BeginBreakPhase()
    {
        CurrentPhase = GamePhase.BreakShot;

        Debug.Log("Break phase started.");
    }

    private void RegisterBallsWithTag(string tagName)
    {
        GameObject[] ballObjects = GameObject.FindGameObjectsWithTag(tagName);

        foreach (GameObject obj in ballObjects)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();

            if (rb != null && !allBalls.Contains(rb))
                allBalls.Add(rb);
        }
    }

    private void CheckBallMovement()
    {
        wasMovingLastFrame = localBallsMoving;
        localBallsMoving = false;

        foreach (Rigidbody rb in allBalls)
        {
            if (rb == null)
                continue;

            if (rb.linearVelocity.magnitude > 0.08f ||
                rb.angularVelocity.magnitude > 0.4f)
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
        if (CurrentPhase == GamePhase.GameOver)
            return;

        ballWasPocketedThisTurn = false;

        playerWhoTookShotWasPlayer1 = isPlayer1Turn;

        if (CurrentPhase == GamePhase.BreakShot)
        {
            breakShotTaken = true;
            return;
        }

        // During normal play we DO NOT switch turn here.
        // We wait until all balls have stopped moving.
    }

    public void BallPocketed(GameObject ball)
    {
        if (CurrentPhase == GamePhase.GameOver)
            return;

        ballWasPocketedThisTurn = true;

        // Cue ball scratch
        if (ball.CompareTag("CueBall"))
        {
            HandleScratch(ball);
            return;
        }

        // 8-Ball rule
        if (ball.CompareTag("8Ball"))
        {
            Rigidbody eightBallRb = ball.GetComponent<Rigidbody>();

            if (eightBallRb != null)
                allBalls.Remove(eightBallRb);

            Destroy(ball);

            int winner = playerWhoTookShotWasPlayer1 ? 2 : 1;

            Debug.Log($"8-Ball pocketed! Player {winner} wins!");

            EndGame(winner);

            return;
        }

        // Normal scoring
        if (playerWhoTookShotWasPlayer1)
        {
            player1Score++;

            Debug.Log($"Player 1 Score: {player1Score}");

            if (player1Score >= scoreToWin)
            {
                EndGame(1);
                return;
            }
        }
        else
        {
            player2Score++;

            Debug.Log($"Player 2 Score: {player2Score}");

            if (player2Score >= scoreToWin)
            {
                EndGame(2);
                return;
            }
        }

        Rigidbody rb = ball.GetComponent<Rigidbody>();

        if (rb != null)
            allBalls.Remove(rb);

        Destroy(ball);
    }

    public void EndGame(int winner)
    {
        if (CurrentPhase == GamePhase.GameOver)
            return;

        WinningPlayer = winner;

        CurrentPhase = GamePhase.GameOver;

        Debug.Log($"PLAYER {winner} WINS!");
    }

    private void OnBallsStoppedMoving()
    {
        if (CurrentPhase == GamePhase.GameOver)
            return;

        if (CurrentPhase == GamePhase.BreakShot)
        {
            EndBreakShot();
            return;
        }

        Debug.Log("Balls stopped. Switching turn.");

        SwitchTurn();
    }

    private void EndBreakShot()
    {
        if (!breakShotTaken)
            return;

        CurrentPhase = GamePhase.NormalPlay;

        SwitchTurn();
    }

    public void SwitchTurn()
    {
        if (CurrentPhase == GamePhase.GameOver)
            return;

        isPlayer1Turn = !isPlayer1Turn;

        ClearDefenderPads();
        
        TurnNumber++;

        OnTurnStarted?.Invoke();

        Debug.Log(
            $"It is now Player {(isPlayer1Turn ? "1" : "2")}'s turn."
        );
    }

    private void HandleScratch(GameObject cueBall)
    {
        RespawnCueBall(cueBall);
    }

    private void RespawnCueBall(GameObject cueBall)
    {
        cueBall.transform.position =
            new Vector3(0f, 0.5f, -2f);

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

    private void ClearDefenderPads()
    {
        GameObject[] pads =
            GameObject.FindGameObjectsWithTag("DefenderPad");

        foreach (GameObject pad in pads)
        {
            Destroy(pad);
        }
    }
}