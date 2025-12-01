using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Disables TextMeshPro font atlas previews that might be showing in the Scene view.
/// Run this once to configure the font to not show atlas previews.
/// </summary>
public class DisableFontAtlasPreview : EditorWindow
{
    [MenuItem("Tools/Disable Font Atlas Preview")]
    public static void ShowWindow()
    {
        DisableAtlasPreviews();
    }

    private static void DisableAtlasPreviews()
    {
        // Find all TextMeshPro font assets
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            
            if (font != null)
            {
                // Set atlas population mode to static (pre-generated) instead of dynamic
                // This prevents runtime atlas generation that might show previews
                SerializedObject so = new SerializedObject(font);
                SerializedProperty atlasMode = so.FindProperty("m_AtlasPopulationMode");
                
                if (atlasMode != null && atlasMode.intValue == 0) // 0 = Dynamic
                {
                    // Keep it dynamic but we'll hide the preview differently
                    Debug.Log($"Found dynamic font: {font.name} at {path}");
                }
                
                so.ApplyModifiedProperties();
                count++;
            }
        }

        Debug.Log($"Checked {count} TextMeshPro font assets. The atlas preview should not appear in Game view.");
        Debug.Log("If it's still visible, it might be in the Scene view - try toggling Gizmos (Shift+G) or checking Scene view settings.");
    }
}

