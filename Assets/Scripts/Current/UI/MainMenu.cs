using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using CardGame.Networking;

namespace CardGame.UI
{
    /// <summary>
    /// Main menu controller with lobby integration
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private LobbyUI lobbyUI;

        [Header("Settings")]
        [SerializeField] private string singlePlayerSceneName = "BattleScreenSinglePlayer";
        [SerializeField] private string multiplayerSceneName = "BattleScreenMultiplayer";
        [SerializeField] private string settingsSceneName = "Settings";
        [SerializeField] private string deckCollectionSceneName = "DeckCollection";

        private LobbyUI runtimeLobbyUI;

        private void Start()
        {
            // Ensure NetworkManager and LobbyManager exist
            EnsureNetworkManagers();
        }

        private void EnsureNetworkManagers()
        {
            // Create NetworkManager if it doesn't exist
            if (NetworkManager.Instance == null)
            {
                GameObject networkManagerObj = new GameObject("NetworkManager");
                networkManagerObj.AddComponent<NetworkManager>();
            }

            // Create LobbyManager if it doesn't exist
            if (LobbyManager.Instance == null)
            {
                GameObject lobbyManagerObj = new GameObject("LobbyManager");
                lobbyManagerObj.AddComponent<LobbyManager>();
            }
        }

        /// <summary>
        /// Loads a scene by name (legacy method for button compatibility)
        /// </summary>
        public void GoToScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Starts single player game
        /// </summary>
        public void StartSinglePlayer()
        {
            SceneManager.LoadScene(singlePlayerSceneName);
        }

        /// <summary>
        /// Opens the multiplayer lobby UI
        /// </summary>
        public void OpenMultiplayerLobby()
        {
            LobbyUI targetLobbyUI = EnsureLobbyUI();
            if (targetLobbyUI != null)
            {
                targetLobbyUI.ShowLobby();
            }
            else
            {
                Debug.LogWarning("[MainMenu] LobbyUI is not available. Loading multiplayer scene directly...");
                SceneManager.LoadScene(multiplayerSceneName);
            }
        }

        private LobbyUI EnsureLobbyUI()
        {
            if (lobbyUI != null)
            {
                return lobbyUI;
            }

            if (runtimeLobbyUI != null)
            {
                return runtimeLobbyUI;
            }

            runtimeLobbyUI = CreateRuntimeLobbyUI();
            return runtimeLobbyUI;
        }

        private LobbyUI CreateRuntimeLobbyUI()
        {
            RectTransform canvasRect = transform as RectTransform;
            if (canvasRect == null)
            {
                Debug.LogError("[MainMenu] Unable to create runtime lobby UI - MainMenu is not on a Canvas GameObject.");
                return null;
            }

            GameObject panelObject = new GameObject("LobbyPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(canvasRect, false);
            panelObject.SetActive(false);

            const float panelWidth = 760f;
            const float panelHeight = 540f;
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.92f);

            VerticalLayoutGroup panelLayout = panelObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.childAlignment = TextAnchor.MiddleCenter;
            panelLayout.spacing = 18f;
            panelLayout.padding = new RectOffset(32, 32, 32, 32);

            // Title
            TextMeshProUGUI title = CreateTMPText(
                panelRect,
                "LobbyTitle",
                "Multiplayer Lobby",
                40,
                Vector2.zero,
                new Vector2(panelWidth - 160f, 70f),
                TextAlignmentOptions.Center);

            CreateDivider(panelRect, Vector2.zero, panelWidth - 200f, 2f, new Color(0.7f, 0.7f, 0.7f, 0.9f));

            // Input field
            TMP_InputField roomNameInput = CreateRoomNameInput(
                panelRect,
                Vector2.zero,
                new Vector2(panelWidth - 220f, 56f));

            // Button grid
            RectTransform buttonGrid = CreateSection(panelRect, "LobbyButtonGrid", panelWidth - 200f, 200f);
            GridLayoutGroup grid = buttonGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2((panelWidth - 220f - grid.spacing.x) / 2f, 56f);
            grid.spacing = new Vector2(16f, 12f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            Button createButton = CreateButton(buttonGrid, "CreateLobbyButton", "Create Lobby", Vector2.zero, new Vector2(0f, 56f), true);
            Button joinButton = CreateButton(buttonGrid, "JoinLobbyButton", "Join Lobby", Vector2.zero, new Vector2(0f, 56f), true);
            Button joinRandomButton = CreateButton(buttonGrid, "JoinRandomButton", "Quick Join", Vector2.zero, new Vector2(0f, 56f), true);
            Button backButton = CreateButton(buttonGrid, "BackButton", "Back", Vector2.zero, new Vector2(0f, 56f), true);

            // Room list area
            RectTransform roomListContainer = CreateRoomListContainer(panelRect, Vector2.zero, new Vector2(panelWidth - 220f, 220f));
            GameObject roomListItemTemplate = CreateRoomListItemTemplate(roomListContainer);
            roomListItemTemplate.SetActive(false);

            // Footer
            TextMeshProUGUI statusText = CreateTMPText(
                panelRect,
                "StatusText",
                "Initializing lobby...",
                22,
                Vector2.zero,
                new Vector2(panelWidth - 160f, 40f),
                TextAlignmentOptions.Center);

            TextMeshProUGUI connectionStatus = CreateTMPText(
                panelRect,
                "ConnectionStatus",
                "Disconnected",
                20,
                Vector2.zero,
                new Vector2(panelWidth - 160f, 34f),
                TextAlignmentOptions.Center);

            LobbyUI lobbyUIComponent = panelObject.AddComponent<LobbyUI>();
            lobbyUIComponent.ConfigureRuntimeReferences(
                panelObject,
                createButton,
                joinButton,
                joinRandomButton,
                backButton,
                roomNameInput,
                statusText,
                connectionStatus,
                roomListContainer,
                roomListItemTemplate);

            return lobbyUIComponent;
        }

        private RectTransform CreateSection(RectTransform parent, string name, float width, float height)
        {
            GameObject sectionObj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = sectionObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private RectTransform CreateDivider(RectTransform parent, Vector2 anchoredPosition, float width, float thickness = 4f, Color? colorOverride = null)
        {
            GameObject dividerObj = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = dividerObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, thickness);
            rect.anchoredPosition = anchoredPosition;

            Image img = dividerObj.GetComponent<Image>();
            img.color = colorOverride ?? new Color(0f, 0f, 0f, 0.35f);

            return rect;
        }

        private TextMeshProUGUI CreateTMPText(RectTransform parent, string name, string text, int fontSize, Vector2 anchoredPosition, Vector2 size, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private Button CreateButton(RectTransform parent, string name, string label, Vector2 anchoredPosition, Vector2? sizeOverride = null, bool layoutControlled = false)
        {
            GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            if (layoutControlled)
            {
                LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = sizeOverride?.y ?? 52f;
                layoutElement.flexibleWidth = 1f;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = sizeOverride ?? new Vector2(260f, 40f);
                rect.anchoredPosition = anchoredPosition;
            }

            Image image = buttonObj.GetComponent<Image>();
            image.color = new Color(0.08f, 0.24f, 0.65f, 0.94f);

            TextMeshProUGUI buttonLabel = CreateTMPText(rect, "Label", label, 22, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = Vector2.zero;
            buttonLabel.rectTransform.offsetMax = Vector2.zero;

            Button button = buttonObj.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.4f, 0.8f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.15f, 0.3f, 0.6f, 0.95f);
            button.colors = colors;

            return button;
        }

        private TMP_InputField CreateRoomNameInput(RectTransform parent, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject inputObj = new GameObject("RoomNameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = inputObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image bg = inputObj.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.1f);

            RectTransform viewport = new GameObject("TextViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D)).GetComponent<RectTransform>();
            viewport.SetParent(rect, false);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(10f, 6f);
            viewport.offsetMax = new Vector2(-10f, -6f);

            TextMeshProUGUI placeholder = CreateTMPText(viewport, "Placeholder", "Enter room name", 22, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            placeholder.color = new Color(1f, 1f, 1f, 0.5f);
            placeholder.rectTransform.anchorMin = Vector2.zero;
            placeholder.rectTransform.anchorMax = Vector2.one;
            placeholder.rectTransform.offsetMin = Vector2.zero;
            placeholder.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI textComponent = CreateTMPText(viewport, "Text", string.Empty, 22, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            textComponent.rectTransform.anchorMin = Vector2.zero;
            textComponent.rectTransform.anchorMax = Vector2.one;
            textComponent.rectTransform.offsetMin = Vector2.zero;
            textComponent.rectTransform.offsetMax = Vector2.zero;

            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textViewport = viewport;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholder;
            inputField.characterLimit = 24;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            return inputField;
        }

        private RectTransform CreateRoomListContainer(RectTransform parent, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject containerObj = new GameObject("RoomListContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = containerObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image bg = containerObj.GetComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.05f);

            VerticalLayoutGroup layout = containerObj.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);

            ContentSizeFitter fitter = containerObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rect;
        }

        private GameObject CreateRoomListItemTemplate(RectTransform parent)
        {
            Button itemButton = CreateButton(parent, "RoomListItemTemplate", "Room Name (1/2)", Vector2.zero, new Vector2(0f, 40f), true);
            itemButton.gameObject.SetActive(false);
            return itemButton.gameObject;
        }

        /// <summary>
        /// Opens settings scene
        /// </summary>
        public void OpenSettings()
        {
            SceneManager.LoadScene(settingsSceneName);
        }

        /// <summary>
        /// Opens deck collection scene
        /// </summary>
        public void OpenDeckCollection()
        {
            SceneManager.LoadScene(deckCollectionSceneName);
        }

        /// <summary>
        /// Exits the game
        /// </summary>
        public void ExitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
