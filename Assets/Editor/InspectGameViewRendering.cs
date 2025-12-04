using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Inspects what's actually being rendered in the Game view to find the atlas display.
/// </summary>
public class InspectGameViewRendering : EditorWindow
{
    [MenuItem("Tools/Inspect Game View Rendering")]
    public static void ShowWindow()
    {
        GetWindow<InspectGameViewRendering>("Inspect Rendering");
    }

    private void OnGUI()
    {
        GUILayout.Label("Inspect Game View Rendering", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            GUILayout.Label("Enter Play Mode first!", EditorStyles.helpBox);
            return;
        }

        if (GUILayout.Button("List ALL Visible UI Elements", GUILayout.Height(30)))
        {
            ListAllVisibleUI();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("List ALL Canvas Elements", GUILayout.Height(30)))
        {
            ListAllCanvasElements();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Find Objects with 'Atlas' in Name", GUILayout.Height(30)))
        {
            FindObjectsWithAtlasName();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Disable ALL TextMeshPro Components", GUILayout.Height(30)))
        {
            DisableAllTextMeshPro();
        }
    }

    private void ListAllVisibleUI()
    {
        Debug.Log("=== ALL VISIBLE UI ELEMENTS ===");

        // Get all Canvas elements
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Debug.Log($"Found {canvases.Length} Canvas(es)");

        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"Canvas: {GetFullPath(canvas.gameObject)} - Active: {canvas.gameObject.activeInHierarchy} - RenderMode: {canvas.renderMode}");

            // Get all UI elements in this canvas
            Graphic[] graphics = canvas.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic.gameObject.activeInHierarchy)
                {
                    string info = $"  UI Element: {GetFullPath(graphic.gameObject)} - Type: {graphic.GetType().Name}";
                    
                    if (graphic is Image img && img.sprite != null)
                    {
                        info += $" - Sprite: {img.sprite.name}";
                    }
                    else if (graphic is RawImage rawImg && rawImg.texture != null)
                    {
                        info += $" - Texture: {rawImg.texture.name}";
                    }
                    else if (graphic is TMP_Text tmpText)
                    {
                        info += $" - Text: '{tmpText.text}' - Color: {tmpText.color}";
                    }

                    Debug.Log(info, graphic.gameObject);
                }
            }
        }
    }

    private void ListAllCanvasElements()
    {
        Debug.Log("=== ALL CANVAS ELEMENTS (Including Inactive) ===");

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"\n=== Canvas: {GetFullPath(canvas.gameObject)} ===");
            
            // Get ALL children recursively
            List<GameObject> allChildren = new List<GameObject>();
            GetAllChildren(canvas.transform, allChildren);

            foreach (GameObject child in allChildren)
            {
                string components = "";
                
                if (child.GetComponent<Image>() != null) components += " [Image]";
                if (child.GetComponent<RawImage>() != null) components += " [RawImage]";
                if (child.GetComponent<TMP_Text>() != null) components += " [TMP_Text]";
                if (child.GetComponent<Text>() != null) components += " [Text]";
                if (child.GetComponent<Renderer>() != null) components += " [Renderer]";

                Debug.Log($"  {GetFullPath(child)} - Active: {child.activeInHierarchy}{components}", child);
            }
        }
    }

    private void GetAllChildren(Transform parent, List<GameObject> list)
    {
        list.Add(parent.gameObject);
        foreach (Transform child in parent)
        {
            GetAllChildren(child, list);
        }
    }

    private void FindObjectsWithAtlasName()
    {
        Debug.Log("=== Objects with 'Atlas' in Name ===");

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        int found = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("atlas"))
            {
                Debug.LogWarning($"Found: {GetFullPath(obj)} - Active: {obj.activeInHierarchy}", obj);
                Selection.activeGameObject = obj;
                found++;
            }
        }

        if (found == 0)
        {
            Debug.Log("No objects with 'atlas' in name found.");
        }
    }

    private void DisableAllTextMeshPro()
    {
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        int disabled = 0;

        foreach (TMP_Text text in allTexts)
        {
            if (text.gameObject.activeInHierarchy)
            {
                text.gameObject.SetActive(false);
                disabled++;
            }
        }

        Debug.Log($"Disabled {disabled} TextMeshPro component(s). Check if the atlas disappears now.");
        
        if (disabled > 0)
        {
            EditorUtility.DisplayDialog("TextMeshPro Disabled", 
                $"Disabled {disabled} TextMeshPro component(s).\n\nCheck the Game view - if the atlas is gone, one of these was the culprit.\n\nYou can re-enable them individually to find which one.", 
                "OK");
        }
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


