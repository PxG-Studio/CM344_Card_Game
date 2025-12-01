using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Searches for TextMeshPro or UI elements displaying "0" in orange or green colors.
/// Specifically looks in the board area and score displays.
/// </summary>
public class FindZeroDisplay : EditorWindow
{
    [MenuItem("Tools/Find Orange/Green Zero Display")]
    public static void ShowWindow()
    {
        GetWindow<FindZeroDisplay>("Find Zero Display");
    }

    private void OnGUI()
    {
        GUILayout.Label("Find Orange/Green Zero Display", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Search for '0' with Orange/Green Colors", GUILayout.Height(30)))
        {
            SearchForZeroDisplay();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Search Board Area", GUILayout.Height(30)))
        {
            SearchBoardArea();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Search ScoreUI Components", GUILayout.Height(30)))
        {
            SearchScoreUI();
        }

        GUILayout.Space(10);
        GUILayout.Label("Check Console for results. Objects will be selected.", EditorStyles.helpBox);
    }

    private void SearchForZeroDisplay()
    {
        // Orange color (P1): approximately (1f, 0.62f, 0.27f, 1f) or similar
        Color orangeColor = new Color(1f, 0.62f, 0.27f, 1f);
        // Green color (P2): approximately (0.43f, 1f, 0.55f, 1f) or similar
        Color greenColor = new Color(0.43f, 1f, 0.55f, 1f);

        Debug.Log("=== Searching for TextMeshPro components with '0' in orange or green ===");

        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        int foundCount = 0;

        foreach (TMP_Text text in allTexts)
        {
            if (text.text != null && text.text.Contains("0"))
            {
                Color textColor = text.color;
                
                // Check if color is close to orange or green
                bool isOrange = IsColorSimilar(textColor, orangeColor, 0.2f);
                bool isGreen = IsColorSimilar(textColor, greenColor, 0.2f);

                if (isOrange || isGreen)
                {
                    string colorName = isOrange ? "ORANGE" : "GREEN";
                    Debug.LogWarning($"Found {colorName} '0' display: {GetFullPath(text.gameObject)} - Text: '{text.text}' - Color: {textColor}", text.gameObject);
                    Selection.activeGameObject = text.gameObject;
                    foundCount++;
                }
            }
        }

        // Also check regular UI Text components
        UnityEngine.UI.Text[] uiTexts = FindObjectsOfType<UnityEngine.UI.Text>(true);
        foreach (UnityEngine.UI.Text text in uiTexts)
        {
            if (text.text != null && text.text.Contains("0"))
            {
                Color textColor = text.color;
                bool isOrange = IsColorSimilar(textColor, orangeColor, 0.2f);
                bool isGreen = IsColorSimilar(textColor, greenColor, 0.2f);

                if (isOrange || isGreen)
                {
                    string colorName = isOrange ? "ORANGE" : "GREEN";
                    Debug.LogWarning($"Found {colorName} '0' in UI Text: {GetFullPath(text.gameObject)} - Text: '{text.text}' - Color: {textColor}", text.gameObject);
                    Selection.activeGameObject = text.gameObject;
                    foundCount++;
                }
            }
        }

        if (foundCount == 0)
        {
            Debug.Log("No orange or green '0' displays found. Searching for any '0' displays...");
            // Fallback: find any "0" displays
            foreach (TMP_Text text in allTexts)
            {
                if (text.text != null && text.text.Contains("0"))
                {
                    Debug.Log($"Found '0' display: {GetFullPath(text.gameObject)} - Text: '{text.text}' - Color: {text.color}", text.gameObject);
                }
            }
        }
        else
        {
            Debug.Log($"Found {foundCount} orange/green '0' display(s). Check Console for details.");
        }
    }

    private void SearchBoardArea()
    {
        Debug.Log("=== Searching Board Area ===");

        // Look for common board-related GameObjects
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            string fullPath = GetFullPath(obj);

            // Check if it's in board area (common names)
            if (objName.Contains("board") || 
                objName.Contains("card") && (objName.Contains("drop") || objName.Contains("area")) ||
                objName.Contains("score") ||
                objName.Contains("player1") || objName.Contains("player2") ||
                objName.Contains("p1") || objName.Contains("p2"))
            {
                // Check for TextMeshPro components
                TMP_Text tmpText = obj.GetComponent<TMP_Text>();
                if (tmpText != null && tmpText.text != null && tmpText.text.Contains("0"))
                {
                    Debug.LogWarning($"Found '0' in board area: {fullPath} - Text: '{tmpText.text}' - Color: {tmpText.color}", obj);
                    Selection.activeGameObject = obj;
                }

                // Check children
                TMP_Text[] childTexts = obj.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text childText in childTexts)
                {
                    if (childText.text != null && childText.text.Contains("0"))
                    {
                        Debug.LogWarning($"Found '0' in board area child: {GetFullPath(childText.gameObject)} - Text: '{childText.text}' - Color: {childText.color}", childText.gameObject);
                    }
                }
            }
        }
    }

    private void SearchScoreUI()
    {
        Debug.Log("=== Searching ScoreUI Components ===");

        // Find ScoreUI components
        var scoreUIComponents = FindObjectsOfType<MonoBehaviour>();
        foreach (var comp in scoreUIComponents)
        {
            if (comp.GetType().Name == "ScoreUI")
            {
                Debug.Log($"Found ScoreUI: {GetFullPath(comp.gameObject)}", comp.gameObject);
                
                // Check children for TextMeshPro
                TMP_Text[] texts = comp.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text text in texts)
                {
                    if (text.text != null)
                    {
                        Debug.LogWarning($"  ScoreUI Text: {GetFullPath(text.gameObject)} - Text: '{text.text}' - Color: {text.color}", text.gameObject);
                        if (text.text.Contains("0"))
                        {
                            Selection.activeGameObject = text.gameObject;
                        }
                    }
                }
            }
        }

        // Also search for objects with "Score" in name
        GameObject[] scoreObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in scoreObjects)
        {
            if (obj.name.ToLower().Contains("score"))
            {
                TMP_Text[] texts = obj.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text text in texts)
                {
                    if (text.text != null && text.text.Contains("0"))
                    {
                        Debug.LogWarning($"Found '0' in Score object: {GetFullPath(text.gameObject)} - Text: '{text.text}' - Color: {text.color}", text.gameObject);
                        Selection.activeGameObject = text.gameObject;
                    }
                }
            }
        }
    }

    private bool IsColorSimilar(Color c1, Color c2, float threshold)
    {
        return Mathf.Abs(c1.r - c2.r) < threshold &&
               Mathf.Abs(c1.g - c2.g) < threshold &&
               Mathf.Abs(c1.b - c2.b) < threshold;
    }

    private string GetFullPath(GameObject obj)
    {
        if (obj == null) return "null";
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}

