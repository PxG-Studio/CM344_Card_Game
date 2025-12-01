using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Aggressively searches for ANY component that might be displaying the font atlas texture.
/// Checks materials, Image components, RawImage components, and Renderers.
/// </summary>
public class FindAtlasTextureDisplay : EditorWindow
{
    [MenuItem("Tools/Find Atlas Texture Display (Aggressive)")]
    public static void ShowWindow()
    {
        GetWindow<FindAtlasTextureDisplay>("Find Atlas Texture");
    }

    private void OnGUI()
    {
        GUILayout.Label("Aggressive Atlas Texture Search", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (!Application.isPlaying)
        {
            GUILayout.Label("Enter Play Mode first!", EditorStyles.helpBox);
            return;
        }

        if (GUILayout.Button("Search All Texture Displays", GUILayout.Height(30)))
        {
            SearchAllTextureDisplays();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Find Font Atlas Texture Asset", GUILayout.Height(30)))
        {
            FindFontAtlasTexture();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Hide ALL Suspicious Displays", GUILayout.Height(30)))
        {
            HideAllSuspiciousDisplays();
        }

        GUILayout.Space(10);
        GUILayout.Label("This will search materials, textures, and all renderers.", EditorStyles.helpBox);
    }

    private void SearchAllTextureDisplays()
    {
        Debug.Log("=== AGGRESSIVE SEARCH: All Texture Displays ===");

        // 1. Search all Image components
        Image[] images = FindObjectsOfType<Image>(true);
        Debug.Log($"Checking {images.Length} Image components...");
        foreach (Image img in images)
        {
            if (img.sprite != null)
            {
                CheckTexture(img.sprite.texture, img.gameObject, "Image.sprite");
            }
        }

        // 2. Search all RawImage components
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);
        Debug.Log($"Checking {rawImages.Length} RawImage components...");
        foreach (RawImage rawImg in rawImages)
        {
            if (rawImg.texture != null)
            {
                CheckTexture(rawImg.texture, rawImg.gameObject, "RawImage.texture");
            }
        }

        // 3. Search all Renderers (SpriteRenderer, MeshRenderer, etc.)
        Renderer[] renderers = FindObjectsOfType<Renderer>(true);
        Debug.Log($"Checking {renderers.Length} Renderer components...");
        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null)
            {
                CheckTexture(renderer.sharedMaterial.mainTexture, renderer.gameObject, "Renderer.material.mainTexture");
            }
        }

        // 4. Search TextMeshPro materials
        TMP_Text[] tmpTexts = FindObjectsOfType<TMP_Text>(true);
        Debug.Log($"Checking {tmpTexts.Length} TextMeshPro components...");
        foreach (TMP_Text text in tmpTexts)
        {
            if (text.fontMaterial != null && text.fontMaterial.mainTexture != null)
            {
                CheckTexture(text.fontMaterial.mainTexture, text.gameObject, "TMP_Text.fontMaterial.mainTexture");
            }
            if (text.font != null && text.font.material != null && text.font.material.mainTexture != null)
            {
                CheckTexture(text.font.material.mainTexture, text.gameObject, "TMP_Text.font.material.mainTexture");
            }
        }
    }

    private void CheckTexture(Texture texture, GameObject obj, string source)
    {
        if (texture == null) return;

        string texName = texture.name.ToLower();
        bool isAtlas = texName.Contains("atlas") || 
                      (texName.Contains("tmp") && texName.Contains("font")) ||
                      (texName.Contains("textmesh") && texName.Contains("atlas")) ||
                      (texName.Contains("liberation") && texName.Contains("atlas"));

        if (isAtlas)
        {
            Debug.LogWarning($"FOUND ATLAS TEXTURE: {GetFullPath(obj)} - Source: {source} - Texture: {texture.name}", obj);
            Selection.activeGameObject = obj;
        }
    }

    private void FindFontAtlasTexture()
    {
        Debug.Log("=== Finding Font Atlas Texture Asset ===");

        // Find the LiberationSans font asset
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in fonts)
        {
            if (font.name.ToLower().Contains("liberation"))
            {
                Debug.Log($"Found font: {font.name}");
                
                if (font.material != null && font.material.mainTexture != null)
                {
                    Texture atlasTexture = font.material.mainTexture;
                    Debug.LogWarning($"Font atlas texture: {atlasTexture.name} - Size: {atlasTexture.width}x{atlasTexture.height}");
                    Debug.LogWarning($"This texture might be displayed somewhere. Search for objects using this texture.");
                    
                    // Now search for what's using this texture
                    FindObjectsUsingTexture(atlasTexture);
                }
            }
        }
    }

    private void FindObjectsUsingTexture(Texture targetTexture)
    {
        Debug.Log($"=== Searching for objects using texture: {targetTexture.name} ===");

        // Check all Image components
        Image[] images = FindObjectsOfType<Image>(true);
        foreach (Image img in images)
        {
            if (img.sprite != null && img.sprite.texture == targetTexture)
            {
                Debug.LogError($"Image using atlas texture: {GetFullPath(img.gameObject)}", img.gameObject);
                Selection.activeGameObject = img.gameObject;
            }
        }

        // Check all RawImage components
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);
        foreach (RawImage rawImg in rawImages)
        {
            if (rawImg.texture == targetTexture)
            {
                Debug.LogError($"RawImage using atlas texture: {GetFullPath(rawImg.gameObject)}", rawImg.gameObject);
                Selection.activeGameObject = rawImg.gameObject;
            }
        }

        // Check all Renderers
        Renderer[] renderers = FindObjectsOfType<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture == targetTexture)
            {
                Debug.LogError($"Renderer using atlas texture: {GetFullPath(renderer.gameObject)}", renderer.gameObject);
                Selection.activeGameObject = renderer.gameObject;
            }
        }
    }

    private void HideAllSuspiciousDisplays()
    {
        int hidden = 0;

        // Hide Image components with atlas sprites
        Image[] images = FindObjectsOfType<Image>(true);
        foreach (Image img in images)
        {
            if (img.sprite != null)
            {
                string texName = img.sprite.texture != null ? img.sprite.texture.name.ToLower() : "";
                if (texName.Contains("atlas") && texName.Contains("tmp"))
                {
                    img.gameObject.SetActive(false);
                    Debug.Log($"Hidden Image: {GetFullPath(img.gameObject)}");
                    hidden++;
                }
            }
        }

        // Hide RawImage components with atlas textures
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);
        foreach (RawImage rawImg in rawImages)
        {
            if (rawImg.texture != null)
            {
                string texName = rawImg.texture.name.ToLower();
                if (texName.Contains("atlas") && texName.Contains("tmp"))
                {
                    rawImg.gameObject.SetActive(false);
                    Debug.Log($"Hidden RawImage: {GetFullPath(rawImg.gameObject)}");
                    hidden++;
                }
            }
        }

        Debug.Log($"Hidden {hidden} suspicious display(s).");
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

