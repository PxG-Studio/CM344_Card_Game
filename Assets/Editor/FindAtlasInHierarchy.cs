using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Comprehensive search for what's displaying the font atlas.
/// This searches TextMeshPro components, materials, and all GameObjects.
/// </summary>
public class FindAtlasInHierarchy : EditorWindow
{
    [MenuItem("Tools/Find Atlas Display (Comprehensive)")]
    public static void ShowWindow()
    {
        GetWindow<FindAtlasInHierarchy>("Find Atlas");
    }

    private void OnGUI()
    {
        GUILayout.Label("Comprehensive Atlas Search", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Search All TextMeshPro Components", GUILayout.Height(30)))
        {
            SearchTextMeshPro();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Search All Materials", GUILayout.Height(30)))
        {
            SearchMaterials();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Search All GameObjects", GUILayout.Height(30)))
        {
            SearchAllGameObjects();
        }

        GUILayout.Space(10);
        GUILayout.Label("Check the Console for results", EditorStyles.helpBox);
    }

    private void SearchTextMeshPro()
    {
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        Debug.Log($"=== Searching {allTexts.Length} TextMeshPro components ===");

        foreach (TMP_Text text in allTexts)
        {
            if (text.font != null)
            {
                string fontName = text.font.name.ToLower();
                if (fontName.Contains("liberation") || fontName.Contains("tmp"))
                {
                    Debug.Log($"Found TMP_Text: {GetFullPath(text.gameObject)} - Font: {text.font.name} - Text: '{text.text}' - Color: {text.color}", text.gameObject);
                    
                    // Check if material has atlas texture
                    if (text.fontMaterial != null)
                    {
                        Material mat = text.fontMaterial;
                        if (mat.mainTexture != null)
                        {
                            Debug.Log($"  Material texture: {mat.mainTexture.name}", text.gameObject);
                        }
                    }
                }
            }
        }
    }

    private void SearchMaterials()
    {
        Material[] allMaterials = Resources.FindObjectsOfTypeAll<Material>();
        Debug.Log($"=== Searching {allMaterials.Length} Materials ===");

        foreach (Material mat in allMaterials)
        {
            if (mat.mainTexture != null)
            {
                string texName = mat.mainTexture.name.ToLower();
                if (texName.Contains("atlas") || (texName.Contains("tmp") && texName.Contains("font")))
                {
                    Debug.Log($"Found Material with atlas texture: {mat.name} - Texture: {mat.mainTexture.name}", mat);
                }
            }
        }
    }

    private void SearchAllGameObjects()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        Debug.Log($"=== Searching {allObjects.Length} GameObjects ===");

        int suspiciousCount = 0;
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            
            // Check name
            if (objName.Contains("atlas") && !objName.Contains("emoji"))
            {
                Debug.LogWarning($"Suspicious GameObject name: {GetFullPath(obj)}", obj);
                Selection.activeGameObject = obj;
                suspiciousCount++;
            }

            // Check if it has a renderer with suspicious material
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                Material mat = renderer.sharedMaterial;
                if (mat.mainTexture != null)
                {
                    string texName = mat.mainTexture.name.ToLower();
                    if (texName.Contains("atlas") && texName.Contains("tmp"))
                    {
                        Debug.LogWarning($"GameObject with atlas material: {GetFullPath(obj)} - Material: {mat.name}", obj);
                        Selection.activeGameObject = obj;
                        suspiciousCount++;
                    }
                }
            }
        }

        if (suspiciousCount == 0)
        {
            Debug.Log("No suspicious GameObjects found. The atlas might be a Unity Editor preview or debug visualization.");
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

