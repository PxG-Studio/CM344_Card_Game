using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Safely hides Image/RawImage components that appear to be displaying font atlases.
/// NEVER hides legitimate TextMeshPro text objects - only debug artifacts.
/// </summary>
public class HideFontAtlasDisplay : MonoBehaviour
{
    /// <summary>
    /// Checks if a name matches font atlas patterns (for debug artifacts only).
    /// </summary>
    private static bool IsFontAtlasName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        name = name.ToLower();
        return (name.Contains("atlas") || name.Contains("tmp") || 
                name.Contains("textmesh") || name.Contains("font")) &&
               !name.Contains("emoji");
    }

    /// <summary>
    /// Checks if a GameObject is part of a legitimate TextMeshPro text object.
    /// These should NEVER be hidden as they always use font atlas textures.
    /// </summary>
    private static bool IsTMPTextObject(GameObject go)
    {
        if (go == null) return false;
        
        // Check if this GameObject or any parent has a TMP_Text component
        TMP_Text tmpText = go.GetComponentInParent<TMP_Text>();
        return tmpText != null;
    }

    [ContextMenu("Hide Font Atlas Displays")]
    public void HideAtlasDisplays()
    {
        Image[] images = FindObjectsOfType<Image>(true);
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);

        int hiddenCount = 0;

        foreach (Image img in images)
        {
            // Skip if this is part of a legitimate TMP text object
            if (IsTMPTextObject(img.gameObject))
                continue;

            if (img.sprite != null)
            {
                string spriteName = img.sprite.name.ToLower();
                string texName = img.sprite.texture != null ? img.sprite.texture.name.ToLower() : "";
                
                if (IsFontAtlasName(spriteName) || IsFontAtlasName(texName))
                {
                    img.gameObject.SetActive(false);
                    Debug.Log($"Hidden Image displaying font atlas: {GetFullPath(img.gameObject)}");
                    hiddenCount++;
                }
            }
        }

        foreach (RawImage rawImg in rawImages)
        {
            // Skip if this is part of a legitimate TMP text object
            if (IsTMPTextObject(rawImg.gameObject))
                continue;

            if (rawImg.texture != null)
            {
                string textureName = rawImg.texture.name.ToLower();
                if (IsFontAtlasName(textureName))
                {
                    rawImg.gameObject.SetActive(false);
                    Debug.Log($"Hidden RawImage displaying font atlas: {GetFullPath(rawImg.gameObject)}");
                    hiddenCount++;
                }
            }
        }

        if (hiddenCount > 0)
        {
            Debug.Log($"Hidden {hiddenCount} GameObject(s) displaying font atlases (debug artifacts only).");
        }
        else
        {
            Debug.Log("No font atlas displays found to hide.");
        }
    }

    private string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }

    #if UNITY_EDITOR
    [MenuItem("Tools/Find All Atlas Displays (Select Only)")]
    public static void FindAllAtlasDisplays()
    {
        // Find and select all atlas displays without hiding them
        // NOTE: This excludes TMP text objects to avoid false positives
        List<GameObject> foundObjects = new List<GameObject>();

        // 1. Search all Image components (excluding TMP text)
        Image[] images = FindObjectsOfType<Image>(true);
        foreach (Image img in images)
        {
            // Skip if this is part of a legitimate TMP text object
            if (IsTMPTextObject(img.gameObject))
                continue;

            if (img.sprite != null)
            {
                string spriteName = img.sprite.name.ToLower();
                string texName = img.sprite.texture != null ? img.sprite.texture.name.ToLower() : "";
                if (IsFontAtlasName(spriteName) || IsFontAtlasName(texName))
                {
                    if (!foundObjects.Contains(img.gameObject))
                    {
                        foundObjects.Add(img.gameObject);
                        Debug.LogWarning($"Found Image with atlas: {GetFullPathStatic(img.gameObject)}", img.gameObject);
                    }
                }
            }
        }

        // 2. Search all RawImage components (excluding TMP text)
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);
        foreach (RawImage rawImg in rawImages)
        {
            // Skip if this is part of a legitimate TMP text object
            if (IsTMPTextObject(rawImg.gameObject))
                continue;

            if (rawImg.texture != null)
            {
                string textureName = rawImg.texture.name.ToLower();
                if (IsFontAtlasName(textureName))
                {
                    if (!foundObjects.Contains(rawImg.gameObject))
                    {
                        foundObjects.Add(rawImg.gameObject);
                        Debug.LogWarning($"Found RawImage with atlas: {GetFullPathStatic(rawImg.gameObject)}", rawImg.gameObject);
                    }
                }
            }
        }

        if (foundObjects.Count > 0)
        {
            Selection.objects = foundObjects.ToArray();
            Debug.Log($"Found {foundObjects.Count} GameObject(s) displaying font atlases (excluding TMP text). They are now selected in the hierarchy.");
        }
        else
        {
            Debug.Log("No font atlas displays found (excluding legitimate TMP text).");
        }
    }

    [MenuItem("Tools/Hide Font Atlas Displays")]
    public static void HideAtlasDisplaysStatic()
    {
        // Works in both Edit Mode and Play Mode
        // SAFE: Never hides legitimate TextMeshPro text objects
        int hiddenCount = 0;
        List<GameObject> hiddenObjects = new List<GameObject>();

        // 1. Search all Image components (excluding TMP text)
        Image[] images = FindObjectsOfType<Image>(true);
        foreach (Image img in images)
        {
            // CRITICAL: Skip if this is part of a legitimate TMP text object
            if (IsTMPTextObject(img.gameObject))
                continue;

            if (img.sprite != null)
            {
                string spriteName = img.sprite.name.ToLower();
                string texName = img.sprite.texture != null ? img.sprite.texture.name.ToLower() : "";
                if (IsFontAtlasName(spriteName) || IsFontAtlasName(texName))
                {
                    if (!hiddenObjects.Contains(img.gameObject))
                    {
                        hiddenObjects.Add(img.gameObject);
                        img.gameObject.SetActive(false);
                        Debug.Log($"Hidden Image displaying font atlas: {GetFullPathStatic(img.gameObject)}", img.gameObject);
                        hiddenCount++;
                    }
                }
            }
        }

        // 2. Search all RawImage components (excluding TMP text)
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);
        foreach (RawImage rawImg in rawImages)
        {
            // CRITICAL: Skip if this is part of a legitimate TMP text object
            if (IsTMPTextObject(rawImg.gameObject))
                continue;

            if (rawImg.texture != null)
            {
                string textureName = rawImg.texture.name.ToLower();
                if (IsFontAtlasName(textureName))
                {
                    if (!hiddenObjects.Contains(rawImg.gameObject))
                    {
                        hiddenObjects.Add(rawImg.gameObject);
                        rawImg.gameObject.SetActive(false);
                        Debug.Log($"Hidden RawImage displaying font atlas: {GetFullPathStatic(rawImg.gameObject)}", rawImg.gameObject);
                        hiddenCount++;
                    }
                }
            }
        }

        // NOTE: We do NOT scan Renderers anymore because:
        // - TMP text uses MeshRenderer with font atlas materials
        // - This would incorrectly hide legitimate text labels
        // - Only Image/RawImage debug artifacts should be hidden

        if (hiddenCount > 0)
        {
            Debug.Log($"Hidden {hiddenCount} GameObject(s) displaying font atlases (debug artifacts only - TMP text was protected).");
            if (hiddenObjects.Count > 0)
            {
                Selection.objects = hiddenObjects.ToArray();
            }
        }
        else
        {
            Debug.Log("No font atlas displays found to hide. TMP text objects are protected and will never be hidden.");
        }
    }

    private static string GetFullPathStatic(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
    #endif
}

