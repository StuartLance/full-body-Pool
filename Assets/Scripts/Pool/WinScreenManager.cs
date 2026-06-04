using TMPro;
using UnityEngine;

public class WinScreenManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI winnerText;

    private bool displayed = false;

    private void Start()
    {
        if (winnerText != null)
        {
            winnerText.text = "";
        }
    }

    private void Update()
    {
        if (displayed)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GamePhase.GameOver)
            return;

        displayed = true;

        Debug.Log("WIN SCREEN TRIGGERED");

        winnerText.text =
            $"PLAYER {GameManager.Instance.WinningPlayer}\nWINS!";

        Time.timeScale = 0f;
    }
}