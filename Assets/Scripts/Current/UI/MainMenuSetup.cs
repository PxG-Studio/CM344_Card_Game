using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace CardGame.UI
{
    /// <summary>
    /// Sets up a beautiful main menu with title and properly styled buttons
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MainMenuSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoSetup()
        {
            // Only run in MainMenu scene
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (scene.name == "MainMenu")
            {
                // Check if setup already exists
                if (FindObjectOfType<MainMenuSetup>() == null)
                {
                    GameObject setupObj = new GameObject("MainMenuSetup");
                    setupObj.AddComponent<MainMenuSetup>();
                }
            }
        }

        private void Awake()
        {
            SetupMainMenu();
        }

        private void SetupMainMenu()
        {
            // Find or create canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Configure canvas scaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            Transform canvasTransform = canvas.transform;

            // Create title
            CreateTitle(canvasTransform);

            // Create button container
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(canvasTransform, false);
            RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0, -100);
            containerRect.sizeDelta = new Vector2(400, 400);

            // Add vertical layout
            VerticalLayoutGroup layout = buttonContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Create buttons
            CreateMenuButton(buttonContainer.transform, "PLAY GAME", "BattleScreenMultiplayer");
            CreateMenuButton(buttonContainer.transform, "COLLECTION", "DeckCollection");
            CreateMenuButton(buttonContainer.transform, "SETTINGS", "Settings");
            CreateMenuButton(buttonContainer.transform, "QUIT", null);

            Debug.Log("MainMenuSetup: Beautiful menu created!");
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("GameTitle");
            titleObj.transform.SetParent(parent, false);
            titleObj.layer = 5; // UI layer

            RectTransform rectTransform = titleObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0, -100);
            rectTransform.sizeDelta = new Vector2(800, 150);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "CARD BATTLE";
            titleText.fontSize = 72;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.9f, 0.3f); // Gold color

            // Add shadow/outline
            titleText.enableVertexGradient = true;
            titleText.colorGradient = new VertexGradient(
                new Color(1f, 1f, 0.5f),
                new Color(1f, 1f, 0.5f),
                new Color(1f, 0.7f, 0f),
                new Color(1f, 0.7f, 0f)
            );
        }

        private void CreateMenuButton(Transform parent, string buttonText, string sceneName)
        {
            GameObject buttonObj = new GameObject(buttonText + "Button");
            buttonObj.transform.SetParent(parent, false);
            buttonObj.layer = 5; // UI layer

            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(350, 70);

            // Add button component
            Button button = buttonObj.AddComponent<Button>();
            
            // Add image for button background
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.2f, 1f); // Green
            buttonImage.sprite = CreateButtonSprite();

            // Configure button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.8f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.5f, 0.15f, 1f);
            colors.selectedColor = new Color(0.3f, 0.8f, 0.3f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            // Add button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            textObj.layer = 5;

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 32;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            // Add button functionality
            if (sceneName != null)
            {
                button.onClick.AddListener(() => LoadScene(sceneName));
            }
            else if (buttonText == "QUIT")
            {
                button.onClick.AddListener(QuitGame);
            }
        }

        private Sprite CreateButtonSprite()
        {
            // Create a simple rounded rectangle sprite
            Texture2D texture = new Texture2D(100, 100);
            Color[] pixels = new Color[100 * 100];
            
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    // Create rounded corners
                    float dx = Mathf.Abs(x - 50f);
                    float dy = Mathf.Abs(y - 50f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if ((x < 10 || x > 90 || y < 10 || y > 90) && distance > 40)
                    {
                        pixels[y * 100 + x] = Color.clear;
                    }
                    else
                    {
                        pixels[y * 100 + x] = Color.white;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 100, 100), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(10, 10, 10, 10));
        }

        private void LoadScene(string sceneName)
        {
            Debug.Log($"Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }

        private void QuitGame()
        {
            Debug.Log("Quitting game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

