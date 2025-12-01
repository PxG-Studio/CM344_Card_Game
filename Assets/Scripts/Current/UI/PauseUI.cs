using UnityEngine;
using UnityEngine.UI;
using CardGame.Managers;
using UnityEngine.SceneManagement;
using JetBrains.Annotations;
using System;

namespace CardGame.UI
{
    /// <summary>
    /// Handles the pause menu UI and game pausing logic
    /// </summary>
    public class PauseUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitmenuButton;
        [SerializeField] private Button quitgameButton;
        [SerializeField] private Button restartButton;

        private bool isPaused = false;

        private void Awake()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
            if (quitmenuButton != null)
                quitmenuButton.onClick.AddListener(QuitMenu);
            if (quitgameButton != null)
                quitgameButton.onClick.AddListener(QuitGame);
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            HidePausePanel();
        }

        /// <summary>
        /// Quit to main menu / title screen from the pause menu.
        /// In editor/WebGL this behaves like "Return to Title" rather than closing the app.
        /// </summary>
        private void QuitMenu()
        {
            // Ensure the game is unpaused before leaving the scene
            isPaused = false;
            Time.timeScale = 1f;
            HidePausePanel();

            // If a GameManager exists, allow it to clean up state if needed
            if (GameManager.Instance != null)
            {
                // We don't force a specific state here; GameManager will be re‑initialized
                // when the title/menu scene loads.
            }

            // Return to the title / main menu scene defined in build settings
            // (TitleCard is the configured entry scene).
            SceneManager.LoadScene("TitleCard");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (!isPaused)
                    PauseGame();
                else
                    ResumeGame();
            }
        }

        public void PauseGame()
        {
            isPaused = true;
            ShowPausePanel();
            Time.timeScale = 0f;
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            isPaused = false;
            HidePausePanel();
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.PlayerTurn); // Or restore previous state
        }

        private void ShowPausePanel()
        {
            if (pausePanel != null)
                pausePanel.SetActive(true);
        }

        private void HidePausePanel()
        {
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        /// <summary>
        /// Fully quit the game application.
        /// In the editor this stops play mode; in a build it closes the app.
        /// </summary>
        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Restart the current battle from the pause menu.
        /// </summary>
        public void RestartGame()
        {
            // Clear pause state
            isPaused = false;
            Time.timeScale = 1f;
            HidePausePanel();

            // Prefer the GameManager's rematch/reset logic if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetGameState();
            }
            else
            {
                // Fallback: reload the active scene
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        public void GoToScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}