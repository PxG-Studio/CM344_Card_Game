using CardGame.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardGame.Managers
{
    /// <summary>
    /// Centralized turn control hooked to the HUD "End Turn" button.
    /// Ensures only one player can act at a time and routes button presses to the GameManager.
    /// </summary>
    public class TurnController : MonoBehaviour
    {
        [SerializeField] private Button endTurnButton;
        [SerializeField] private TextMeshProUGUI endTurnLabel;

        private GameState currentState;

        private void Awake()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(HandleEndTurnClicked);
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(HandleEndTurnClicked);
            }
        }

        private void HandleStateChanged(GameState newState)
        {
            currentState = newState;
            UpdateButton();
        }

        private void UpdateButton()
        {
            if (endTurnButton == null) return;

            bool interactable = currentState == GameState.PlayerTurn || currentState == GameState.EnemyTurn;
            endTurnButton.interactable = interactable;

            if (endTurnLabel != null)
            {
                switch (currentState)
                {
                    case GameState.PlayerTurn:
                        endTurnLabel.text = "End Player 1 Turn";
                        break;
                    case GameState.EnemyTurn:
                        endTurnLabel.text = "End Player 2 Turn";
                        break;
                    default:
                        endTurnLabel.text = "End Turn";
                        break;
                }
            }
        }

        private void HandleEndTurnClicked()
        {
            if (GameManager.Instance == null) return;

            if (currentState == GameState.PlayerTurn)
            {
                GameManager.Instance.EndPlayerTurn();
            }
            else if (currentState == GameState.EnemyTurn)
            {
                GameManager.Instance.EndEnemyTurn();
            }
        }
    }
}

