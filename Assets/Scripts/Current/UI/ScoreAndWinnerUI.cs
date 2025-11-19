using UnityEngine;
using TMPro;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Handles score and winner display UI
    /// </summary>
    public class ScoreAndWinnerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerScoreText;
        [SerializeField] private TextMeshProUGUI opponentScoreText;
        [SerializeField] private TextMeshProUGUI winnerText;

        private void Awake()
        {
            // Subscribe to score changes
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScoreDisplay;
            }
            // Hide winner text at start
            if (winnerText != null)
                winnerText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
            }
        }

        private void UpdateScoreDisplay(bool isPlayer, int newScore)
        {
            if (isPlayer && playerScoreText != null)
                playerScoreText.text = $"Player: {newScore}";
            else if (!isPlayer && opponentScoreText != null)
                opponentScoreText.text = $"Opponent: {newScore}";
        }

        public void ShowWinner(string winner)
        {
            if (winnerText != null)
            {
                winnerText.text = winner;
                winnerText.gameObject.SetActive(true);
            }
        }
    }
}
