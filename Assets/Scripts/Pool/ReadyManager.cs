using UnityEngine;
using TMPro;

public class ReadyManager : MonoBehaviour
{
    [Header("Trackers")]
    [SerializeField] private Transform player1Tracker;
    [SerializeField] private Transform player2Tracker;

    [Header("Ready Gesture")]
    [SerializeField] private float readyHeightThreshold = 0.25f;
    [SerializeField] private float holdTime = 1f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI readyText;

    private float player1Timer;
    private float player2Timer;

    private bool player1Ready;
    private bool player2Ready;

    private void Start()
    {
        UpdateReadyText();
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GamePhase.WaitingForPlayers)
            return;

        CheckPlayer1();
        CheckPlayer2();

        UpdateReadyText();

        if (player1Ready && player2Ready)
        {
            readyText.text = "STARTING MATCH...";

            GameManager.Instance.BeginBreakPhase();

            if (readyText != null)
            {
                readyText.gameObject.SetActive(false);
            }

            enabled = false;
        }
    }

    private void CheckPlayer1()
    {
        if (player1Ready)
            return;

        if (player1Tracker.position.y <= readyHeightThreshold)
        {
            player1Timer += Time.deltaTime;

            if (player1Timer >= holdTime)
            {
                player1Ready = true;
                Debug.Log("Player 1 Ready");
            }
        }
        else
        {
            player1Timer = 0f;
        }
    }

    private void CheckPlayer2()
    {
        if (player2Ready)
            return;

        if (player2Tracker.position.y <= readyHeightThreshold)
        {
            player2Timer += Time.deltaTime;

            if (player2Timer >= holdTime)
            {
                player2Ready = true;
                Debug.Log("Player 2 Ready");
            }
        }
        else
        {
            player2Timer = 0f;
        }
    }

    private void UpdateReadyText()
    {
        if (readyText == null)
            return;

        if (player1Ready && player2Ready)
        {
            readyText.text = "STARTING MATCH...";
            return;
        }

        if (player1Ready)
        {
            readyText.text =
                "PLAYER 1 READY ✓\n\n" +
                "PLAYER 2:\n" +
                "LOWER SENSOR FOR 1 SECOND";
            return;
        }

        if (player2Ready)
        {
            readyText.text =
                "PLAYER 2 READY ✓\n\n" +
                "PLAYER 1:\n" +
                "LOWER SENSOR FOR 1 SECOND";
            return;
        }

        readyText.text =
            "LOWER BOTH SENSORS\n\n" +
            "FOR 1 SECOND\n\n" +
            "TO START THE MATCH";
    }
}