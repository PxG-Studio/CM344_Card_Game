using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility to find GameObjects that might be displaying TextMeshPro font atlases.
/// Run this in Play Mode to identify what's showing the atlas texture.
/// </summary>
public class FindFontAtlasDisplay : EditorWindow
{
    [MenuItem("Tools/Find Font Atlas Display")]
    public static void ShowWindow()
    {
        GetWindow<FindFontAtlasDisplay>("Find Font Atlas");
    }

    private void OnGUI()
    {
        GUILayout.Label("Find Font Atlas Display", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Find Image/RawImage with Font Atlas", GUILayout.Height(30)))
        {
            FindAtlasDisplays();
        }

        GUILayout.Space(10);
        GUILayout.Label("This will search for Image and RawImage components", EditorStyles.helpBox);
        GUILayout.Label("that might be displaying TextMeshPro font atlases.", EditorStyles.helpBox);
    }

    private void FindAtlasDisplays()
    {
        // Find all Image components
        Image[] images = FindObjectsOfType<Image>(true);
        RawImage[] rawImages = FindObjectsOfType<RawImage>(true);

        int foundCount = 0;

        foreach (Image img in images)
        {
            if (img.sprite != null)
            {
                string spriteName = img.sprite.name.ToLower();
                if (spriteName.Contains("atlas") || spriteName.Contains("tmp") || 
                    spriteName.Contains("textmesh") || spriteName.Contains("font"))
                {
                    Debug.LogWarning($"Found Image with suspicious sprite: {GetFullPath(img.gameObject)} - Sprite: {img.sprite.name}", img.gameObject);
                    Selection.activeGameObject = img.gameObject;
                    foundCount++;
                }
            }
        }

        foreach (RawImage rawImg in rawImages)
        {
            if (rawImg.texture != null)
            {
                string textureName = rawImg.texture.name.ToLower();
                if (textureName.Contains("atlas") || textureName.Contains("tmp") || 
                    textureName.Contains("textmesh") || textureName.Contains("font"))
                {
                    Debug.LogWarning($"Found RawImage with suspicious texture: {GetFullPath(rawImg.gameObject)} - Texture: {rawImg.texture.name}", rawImg.gameObject);
                    Selection.activeGameObject = rawImg.gameObject;
                    foundCount++;
                }
            }
        }

        if (foundCount == 0)
        {
            Debug.Log("No suspicious Image/RawImage components found. The atlas might be visible in the Scene view or a debug visualization.");
        }
        else
        {
            Debug.Log($"Found {foundCount} potential atlas displays. Check the Console for details.");
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
}

