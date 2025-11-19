using TMPro;
using UnityEngine;

/// <summary>
/// UI component for displaying player scores in real-time.
/// Listens to score updates and refreshes the display.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TMP_Text player1Score;
    [SerializeField] private TMP_Text player2Score;

    [Header("Optional Labels")]
    [SerializeField] private TMP_Text player1Label;
    [SerializeField] private TMP_Text player2Label;

    private ScoreManager scoreManager;

    /// <summary>
    /// Initialize and subscribe to score updates.
    /// </summary>
    private void Start()
    {
        // Find the ScoreManager in the scene
        scoreManager = FindObjectOfType<ScoreManager>();
        
        if (scoreManager == null)
        {
            Debug.LogError("ScoreUI: ScoreManager not found in scene!");
            return;
        }

        // Subscribe to score update events
        scoreManager.OnScoreUpdated += UpdateScoreDisplay;

        // Initialize labels if assigned
        if (player1Label != null)
        {
            player1Label.text = "Player 1";
        }
        if (player2Label != null)
        {
            player2Label.text = "Player 2";
        }

        // Initial score display
        UpdateScoreDisplay(0, 0);
    }

    /// <summary>
    /// Unsubscribe from events when destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnScoreUpdated -= UpdateScoreDisplay;
        }
    }

    /// <summary>
    /// Update the score display for both players.
    /// </summary>
    /// <param name="p1Score">Player 1's score (tiles controlled).</param>
    /// <param name="p2Score">Player 2's score (tiles controlled).</param>
    public void UpdateScoreDisplay(int p1Score, int p2Score)
    {
        if (player1Score != null)
        {
            player1Score.text = p1Score.ToString();
        }
        
        if (player2Score != null)
        {
            player2Score.text = p2Score.ToString();
        }
    }

    /// <summary>
    /// Manually update scores (for testing or external calls).
    /// </summary>
    public void SetScores(int p1Score, int p2Score)
    {
        UpdateScoreDisplay(p1Score, p2Score);
    }
}

