using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Events;

namespace CardGame.UI
{
    /// <summary>
    /// Builds a themed main menu UI at runtime so the scene stays minimal.
    /// Matches the dark/teal palette used by CardBattleMultiplayer.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MainMenuSetup : MonoBehaviour
    {
        private static bool isHooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoHook()
        {
            if (isHooked) return;
            isHooked = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainMenu") return;

            // Avoid duplicates if scene already contains a setup component
            if (FindObjectOfType<MainMenuSetup>() != null) return;

                    GameObject setupObj = new GameObject("MainMenuSetup");
                    setupObj.AddComponent<MainMenuSetup>();
        }

        private void Awake()
        {
            BuildMenu();
        }

        private void BuildMenu()
        {
            // Configure camera background to match multiplayer scene
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color32(8, 14, 26, 255); // deep navy
            }

            // Ensure there is an EventSystem so the buttons are interactable
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            MainMenu menuLogic = canvas.GetComponent<MainMenu>();
            if (menuLogic == null)
            {
                menuLogic = canvas.gameObject.AddComponent<MainMenu>();
                Debug.Log("[MainMenuSetup] Attached MainMenu to Canvas so runtime lobby UI can anchor correctly.");
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            Transform canvasTransform = canvas.transform;

            // Clear any previous auto-generated layout
            Transform old = canvasTransform.Find("GeneratedMenuRoot");
            if (old != null)
            {
                DestroyImmediate(old.gameObject);
            }

            // Dim the main menu when the overlay is active.
            Image canvasBackground = canvas.GetComponent<Image>();
            if (canvasBackground == null)
            {
                canvasBackground = canvas.gameObject.AddComponent<Image>();
                canvasBackground.color = new Color(0f, 0f, 0f, 0.6f);
                canvasBackground.raycastTarget = false;
            }

            GameObject root = new GameObject("GeneratedMenuRoot");
            root.transform.SetParent(canvasTransform, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CreateBackground(root.transform);
            CreateTitle(root.transform);
            CreateButtons(menuLogic, root.transform);
        }

        private void CreateBackground(Transform parent)
        {
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(parent, false);
            RectTransform rect = bg.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = bg.AddComponent<Image>();
            image.color = new Color32(9, 19, 33, 255);
        }

        private void CreateTitle(Transform parent)
        {
            GameObject title = new GameObject("MenuTitle");
            title.transform.SetParent(parent, false);

            RectTransform rect = title.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(900f, 140f);

            TextMeshProUGUI tmp = title.AddComponent<TextMeshProUGUI>();
            tmp.text = "CARD FRONT v1.0";
            tmp.fontSize = 86f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.enableVertexGradient = true;
            tmp.colorGradient = new VertexGradient(
                new Color32(69, 206, 255, 255),
                new Color32(69, 206, 255, 255),
                new Color32(26, 137, 191, 255),
                new Color32(26, 137, 191, 255));
        }

        private void CreateButtons(MainMenu menuLogic, Transform parent)
        {
            GameObject container = new GameObject("ButtonColumn");
            container.transform.SetParent(parent, false);
            RectTransform rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -60f);
            rect.sizeDelta = new Vector2(520f, 520f);

            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            AddButton(container.transform, "Play Game", () => menuLogic.StartSinglePlayer());
            AddButton(container.transform, "Collection", () => menuLogic.OpenDeckCollection());
            AddButton(container.transform, "Play Online", () => menuLogic.OpenMultiplayerLobby());
            AddButton(container.transform, "Settings", () => menuLogic.OpenSettings());
            AddButton(container.transform, "Quit", () => menuLogic.ExitGame());
        }

        private void AddButton(Transform parent, string label, UnityAction onClick)
        {
            GameObject buttonObj = new GameObject(label + "Button", typeof(RectTransform), typeof(CanvasRenderer));
            buttonObj.transform.SetParent(parent, false);

            LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 72f;
            layoutElement.minHeight = 72f;

            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 72f); // width controlled by layout

            Image image = buttonObj.AddComponent<Image>();
            image.sprite = CreateRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color32(24, 95, 142, 230);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(24, 95, 142, 230);
            colors.highlightedColor = new Color32(37, 140, 201, 240);
            colors.pressedColor = new Color32(18, 73, 110, 230);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color32(40, 40, 40, 120);
            button.colors = colors;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label.ToUpperInvariant();
            tmp.fontSize = 32f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
        }

        private Sprite CreateRoundedSprite()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[size * size];
            float radius = size / 2f;
            float cornerRadius = radius - 6f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Min(Mathf.Abs(x - radius + 0.5f), cornerRadius);
                    float dy = Mathf.Min(Mathf.Abs(y - radius + 0.5f), cornerRadius);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * size + x] = dist <= cornerRadius ? Color.white : Color.clear;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(16f, 16f, 16f, 16f));
            return sprite;
        }
    }
}

