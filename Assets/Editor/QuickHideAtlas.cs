using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quick utility to find and hide the font atlas display.
/// Press Ctrl+Shift+H (Cmd+Shift+H on Mac) in Play Mode to hide it.
/// </summary>
public class QuickHideAtlas : EditorWindow
{
    private static bool isHidden = false;

    [MenuItem("Tools/Quick Hide Atlas %#h")]
    public static void ToggleAtlasDisplay()
    {
        if (Application.isPlaying)
        {
            HideAllAtlasDisplays();
        }
        else
        {
            Debug.LogWarning("This tool only works in Play Mode. Enter Play Mode first.");
        }
    }

    private static void HideAllAtlasDisplays()
    {
        // Method 1: Hide Image components with atlas textures
        Image[] images = FindObjectsOfType<Image>(true);
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);
        int hidden = 0;

        foreach (Image img in images)
        {
            if (img.sprite != null && IsAtlasTexture(img.sprite.name))
            {
                img.gameObject.SetActive(false);
                hidden++;
            }
        }

        foreach (RawImage rawImg in rawImages)
        {
            if (rawImg.texture != null && IsAtlasTexture(rawImg.texture.name))
            {
                rawImg.gameObject.SetActive(false);
                hidden++;
            }
        }

        // Method 2: Hide any GameObject with "atlas" in the name
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("atlas") && 
                !obj.name.ToLower().Contains("emoji") &&
                obj.activeInHierarchy)
            {
                obj.SetActive(false);
                hidden++;
            }
        }

        if (hidden > 0)
        {
            Debug.Log($"Hidden {hidden} GameObject(s) that might be displaying the font atlas.");
            isHidden = true;
        }
        else
        {
            Debug.Log("No atlas displays found. The atlas might be visible in Scene view - try toggling Gizmos (Shift+G) or check Scene view settings.");
        }
    }

    private static bool IsAtlasTexture(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        string lower = name.ToLower();
        return lower.Contains("atlas") || 
               (lower.Contains("tmp") && lower.Contains("font")) ||
               (lower.Contains("textmesh") && lower.Contains("atlas"));
    }

    [InitializeOnLoadMethod]
    static void OnLoad()
    {
        // Auto-hide on play mode enter
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Small delay to let everything initialize
            EditorApplication.delayCall += () => {
                if (Application.isPlaying && !isHidden)
                {
                    HideAllAtlasDisplays();
                }
            };
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            isHidden = false;
        }
    }
}

