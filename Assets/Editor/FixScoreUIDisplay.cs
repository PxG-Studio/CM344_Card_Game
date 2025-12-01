using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Fixes or hides the ScoreUI TextMeshPro components that are displaying the font atlas.
/// </summary>
public class FixScoreUIDisplay : EditorWindow
{
    [MenuItem("Tools/Fix ScoreUI Atlas Display")]
    public static void ShowWindow()
    {
        GetWindow<FixScoreUIDisplay>("Fix ScoreUI");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix ScoreUI Font Atlas Display", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            GUILayout.Label("Enter Play Mode first!", EditorStyles.helpBox);
            return;
        }

        if (GUILayout.Button("Hide Player1Score", GUILayout.Height(30)))
        {
            HideScoreComponent("Player1Score");
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Hide Player2Score", GUILayout.Height(30)))
        {
            HideScoreComponent("Player2Score");
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Hide Both Score Displays", GUILayout.Height(30)))
        {
            HideScoreComponent("Player1Score");
            HideScoreComponent("Player2Score");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Fix TextMeshPro Rendering", GUILayout.Height(30)))
        {
            FixTextMeshProRendering();
        }

        GUILayout.Space(10);
        GUILayout.Label("Options:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Disable Canvas Group (Make Invisible)", GUILayout.Height(25)))
        {
            DisableScoreUICanvasGroup();
        }

        if (GUILayout.Button("Set Alpha to 0 (Make Transparent)", GUILayout.Height(25)))
        {
            SetScoreUITransparent();
        }
    }

    private void HideScoreComponent(string componentName)
    {
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        
        foreach (TMP_Text text in allTexts)
        {
            if (text.gameObject.name == componentName)
            {
                text.gameObject.SetActive(false);
                Debug.Log($"Hidden {componentName}: {GetFullPath(text.gameObject)}");
                Selection.activeGameObject = text.gameObject;
            }
        }
    }

    private void FixTextMeshProRendering()
    {
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        int fixedCount = 0;

        foreach (TMP_Text text in allTexts)
        {
            if (text.gameObject.name == "Player1Score" || text.gameObject.name == "Player2Score")
            {
                // Force text update
                text.ForceMeshUpdate();
                
                // Ensure material is correct
                if (text.font != null && text.fontMaterial != null)
                {
                    text.fontMaterial = text.font.material;
                }

                // Reset color to ensure it's correct
                if (text.gameObject.name == "Player1Score")
                {
                    text.color = new Color(1f, 0.62f, 0.27f, 1f); // Orange
                }
                else if (text.gameObject.name == "Player2Score")
                {
                    text.color = new Color(0.43f, 1f, 0.55f, 1f); // Green
                }

                Debug.Log($"Fixed TextMeshPro rendering for {text.gameObject.name}");
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            Debug.Log($"Fixed {fixedCount} TextMeshPro component(s).");
        }
        else
        {
            Debug.LogWarning("No Player1Score or Player2Score components found to fix.");
        }
    }

    private void DisableScoreUICanvasGroup()
    {
        GameObject scoreUI = GameObject.Find("ScoreUI");
        if (scoreUI != null)
        {
            CanvasGroup cg = scoreUI.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = scoreUI.AddComponent<CanvasGroup>();
            }
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            Debug.Log("Disabled ScoreUI CanvasGroup - scores are now invisible.");
        }
        else
        {
            Debug.LogWarning("ScoreUI GameObject not found.");
        }
    }

    private void SetScoreUITransparent()
    {
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        int fixedCount = 0;

        foreach (TMP_Text text in allTexts)
        {
            if (text.gameObject.name == "Player1Score" || text.gameObject.name == "Player2Score")
            {
                Color c = text.color;
                c.a = 0f;
                text.color = c;
                fixedCount++;
            }
        }

        Debug.Log($"Set {fixedCount} score text component(s) to transparent.");
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

