using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardGame.Managers;

namespace CardGame.UI
{
    /// <summary>
    /// Main game UI controller
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;
        
        [Header("Game Info")]
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI deckCountText;
        [SerializeField] private TextMeshProUGUI discardCountText;
        
        [Header("Buttons")]
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;
        
        private DeckManager deckManager;
        
        private void Start()
        {
            deckManager = FindObjectOfType<DeckManager>();
            
            // Subscribe to game manager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            
            // Setup button listeners
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(EndTurn);
                
            if (startGameButton != null)
                startGameButton.onClick.AddListener(StartGame);
                
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
                
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
            
            // Initial state
            ShowPanel(menuPanel);
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
        
        private void Update()
        {
            UpdateGameInfo();
        }
        
        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Menu:
                    ShowPanel(menuPanel);
                    break;
                    
                case GameState.Preparing:
                case GameState.PlayerTurn:
                case GameState.EnemyTurn:
                    ShowPanel(gamePanel);
                    UpdateTurnDisplay(newState);
                    break;
                    
                case GameState.Victory:
                    ShowPanel(victoryPanel);
                    break;
                    
                case GameState.Defeat:
                    ShowPanel(defeatPanel);
                    break;
            }
        }
        
        private void ShowPanel(GameObject panel)
        {
            if (menuPanel != null) menuPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
            
            if (panel != null) panel.SetActive(true);
        }
        
        private void UpdateTurnDisplay(GameState state)
        {
            if (turnText == null) return;
            
            switch (state)
            {
                case GameState.PlayerTurn:
                    turnText.text = "Your Turn";
                    turnText.color = Color.green;
                    if (endTurnButton != null) endTurnButton.interactable = true;
                    break;
                    
                case GameState.EnemyTurn:
                    turnText.text = "Enemy Turn";
                    turnText.color = Color.red;
                    if (endTurnButton != null) endTurnButton.interactable = false;
                    break;
                    
                default:
                    turnText.text = "";
                    break;
            }
        }
        
        private void UpdateGameInfo()
        {
            if (deckManager != null)
            {
                if (deckCountText != null)
                    deckCountText.text = $"Deck: {deckManager.DrawPileCount}";
                    
                if (discardCountText != null)
                    discardCountText.text = $"Discard: {deckManager.DiscardPileCount}";
            }
        }
        
        private void EndTurn()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.PlayerTurn)
            {
                GameManager.Instance.EndPlayerTurn();
            }
        }
        
        private void StartGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }
        
        private void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
        
        private void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}

