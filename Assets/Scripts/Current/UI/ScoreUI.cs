using TMPro;
using UnityEngine;
using CardGame.Managers;

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
    // Tracks whether scores have been explicitly set via SetScores before Start()
    // so that Start() doesn't overwrite test-driven values.
    private bool scoresManuallySet = false;

    /// <summary>
    /// Initialize and subscribe to score updates.
    /// </summary>
    private void Start()
    {
        // Find the ScoreManager in the scene
        scoreManager = FindObjectOfType<ScoreManager>();
        
        if (scoreManager == null)
        {
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

        // Initial score display should reflect the current scores from ScoreManager,
        // but ONLY if scores haven't already been driven manually via SetScores()
        // (e.g., in isolated UI tests that don't use ScoreManager at all).
        if (!scoresManuallySet)
        {
            int initialP1 = scoreManager.P1Score;
            int initialP2 = scoreManager.P2Score;
            UpdateScoreDisplay(initialP1, initialP2);
        }
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
    /// Marks the UI as manually-driven so Start() won't overwrite values.
    /// </summary>
    public void SetScores(int p1Score, int p2Score)
    {
        scoresManuallySet = true;
        UpdateScoreDisplay(p1Score, p2Score);
    }
}

