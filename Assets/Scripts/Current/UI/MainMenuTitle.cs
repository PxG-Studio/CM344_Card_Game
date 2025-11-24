using UnityEngine;
using TMPro;

namespace CardGame.UI
{
    /// <summary>
    /// Sets the main menu title text
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class MainMenuTitle : MonoBehaviour
    {
        [Header("Title Settings")]
        [SerializeField] private string titleText = "CARD FRONT v1.0";

        private TextMeshProUGUI titleTextComponent;

        private void Awake()
        {
            titleTextComponent = GetComponent<TextMeshProUGUI>();
            if (titleTextComponent != null)
            {
                titleTextComponent.text = titleText;
            }
        }

        /// <summary>
        /// Updates the title text
        /// </summary>
        public void SetTitle(string newTitle)
        {
            titleText = newTitle;
            if (titleTextComponent != null)
            {
                titleTextComponent.text = titleText;
            }
        }
    }
}

