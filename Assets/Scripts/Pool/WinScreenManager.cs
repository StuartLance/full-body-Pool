using TMPro;
using UnityEngine;

public class WinScreenManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI winnerText;

    private bool hasDisplayed = false;

    private void Update()
    {
        if (hasDisplayed)
            return;

        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GamePhase.GameOver)
            return;

        hasDisplayed = true;

        winnerText.gameObject.SetActive(true);

        winnerText.text =
            $"PLAYER {GameManager.Instance.WinningPlayer}\nWINS!";
    }
}