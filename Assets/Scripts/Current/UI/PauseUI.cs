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

        private void QuitMenu()
        {
            Application.Quit();


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

        private void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            Application.Quit();
        }

        public void RestartGame()
        {

        }
        public void GoToScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}