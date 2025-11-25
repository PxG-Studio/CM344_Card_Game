#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace CardGame.Editor
{
    /// <summary>
    /// Editor script to generate a Persona 5-style cel-shaded card back sprite.
    /// Creates a deep crimson base with gold accents, geometric patterns, and bold black outlines.
    /// </summary>
    public class CardBackGenerator : EditorWindow
    {
        [MenuItem("Tools/CardGame/Generate Card Back Sprite")]
        public static void ShowWindow()
        {
            GetWindow<CardBackGenerator>("Card Back Generator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Persona 5-Style Card Back Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "This will generate a cel-shaded card back sprite at:\n" +
                "Assets/Sprite/CardBack_Default.png\n\n" +
                "Design: Deep crimson base, gold accents, geometric patterns, bold black outlines.",
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Card Back (512x512)", GUILayout.Height(40)))
            {
                GenerateCardBack(512);
            }
            
            if (GUILayout.Button("Generate Card Back (1024x1024)", GUILayout.Height(40)))
            {
                GenerateCardBack(1024);
            }
        }
        
        private void GenerateCardBack(int size)
        {
            // Create texture
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            
            // Define colors (Persona 5 style)
            Color deepCrimson = new Color(0.545f, 0f, 0f, 1f);        // #8B0000 - Base color
            Color gold = new Color(1f, 0.843f, 0f, 1f);              // #FFD700 - Accents
            Color darkGold = new Color(0.8f, 0.6f, 0f, 1f);         // Darker gold
            Color black = Color.black;                               // Outlines
            Color darkRed = new Color(0.4f, 0f, 0f, 1f);            // Darker red for depth
            
            // Fill with base crimson color
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = deepCrimson;
            }
            
            // Draw geometric patterns
            DrawCelShadedCardBack(pixels, size, deepCrimson, gold, darkGold, black, darkRed);
            
            // Apply pixels
            texture.SetPixels(pixels);
            texture.Apply();
            
            // Save to file
            byte[] pngData = texture.EncodeToPNG();
            string path = "Assets/Sprite/CardBack_Default.png";
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Write via temp file to avoid Unity seeing a half-written asset
            string tempPath = path + ".tmp";
            File.WriteAllBytes(tempPath, pngData);
            
            if (File.Exists(path))
            {
                FileUtil.ReplaceFile(tempPath, path);
                File.Delete(tempPath);
            }
            else
            {
                File.Move(tempPath, path);
            }
            
            // Force Unity to detect and import the new file
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            // Use delayCall to configure import settings after Unity processes the file
            EditorApplication.delayCall += () =>
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 100;
                    importer.filterMode = FilterMode.Point; // Sharp cel-shaded look
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.maxTextureSize = 2048;
                    importer.mipmapEnabled = false;
                    
                    // Apply settings
                    EditorUtility.SetDirty(importer);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    AssetDatabase.SaveAssets();
                    
                    Debug.Log($"✅ Card back sprite import settings configured for: {path}");
                }
                else
                {
                    Debug.LogWarning($"CardBackGenerator: Could not get TextureImporter. Please manually reimport: Right-click {path} → Reimport");
                }
            };
            
            Debug.Log($"✅ Card back generated at: {path} ({size}x{size})");
            EditorUtility.DisplayDialog("Success", $"Card back generated at:\n{path}\n\n({size}x{size} pixels)", "OK");
        }
        
        private void DrawCelShadedCardBack(Color[] pixels, int size, Color baseColor, Color gold, Color darkGold, Color black, Color darkRed)
        {
            float centerX = size * 0.5f;
            float centerY = size * 0.5f;
            float borderWidth = size * 0.05f;
            float cornerRadius = size * 0.1f;
            
            // Draw bold black border (cel-shaded style - hard edges)
            DrawBoldBorder(pixels, size, borderWidth, black);
            
            // Draw corner decorative elements
            float cornerSize = size * 0.15f;
            DrawCornerDecoration(pixels, size, 0, 0, cornerSize, gold, black);
            DrawCornerDecoration(pixels, size, size - cornerSize, 0, cornerSize, gold, black);
            DrawCornerDecoration(pixels, size, 0, size - cornerSize, cornerSize, gold, black);
            DrawCornerDecoration(pixels, size, size - cornerSize, size - cornerSize, cornerSize, gold, black);
            
            // Draw central geometric pattern
            DrawCentralPattern(pixels, size, centerX, centerY, gold, darkGold, black);
            
            // Draw side decorative bars
            DrawSideBars(pixels, size, gold, black);
        }
        
        private void DrawBoldBorder(Color[] pixels, int size, float borderWidth, Color borderColor)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Check if pixel is in border region (hard edges - cel-shaded style)
                    if (x < borderWidth || x >= size - borderWidth || 
                        y < borderWidth || y >= size - borderWidth)
                    {
                        int index = y * size + x;
                        pixels[index] = borderColor;
                    }
                }
            }
        }
        
        private void DrawCornerDecoration(Color[] pixels, int size, float startX, float startY, float cornerSize, Color accent, Color outline)
        {
            int startXi = Mathf.RoundToInt(startX);
            int startYi = Mathf.RoundToInt(startY);
            int cornerSizei = Mathf.RoundToInt(cornerSize);
            
            // Draw geometric L-shape corner pattern
            for (int y = startYi; y < startYi + cornerSizei && y < size; y++)
            {
                for (int x = startXi; x < startXi + cornerSizei && x < size; x++)
                {
                    float localX = x - startX;
                    float localY = y - startY;
                    
                    // L-shape pattern
                    bool isPattern = (localX < cornerSize * 0.3f) || (localY < cornerSize * 0.3f);
                    
                    if (isPattern)
                    {
                        // Thick outline
                        if (localX < 3 || localY < 3 || 
                            localX > cornerSize * 0.3f - 3 || localY > cornerSize * 0.3f - 3)
                        {
                            int index = y * size + x;
                            if (index >= 0 && index < pixels.Length)
                            {
                                pixels[index] = outline;
                            }
                        }
                        else
                        {
                            int index = y * size + x;
                            if (index >= 0 && index < pixels.Length)
                            {
                                pixels[index] = accent;
                            }
                        }
                    }
                }
            }
        }
        
        private void DrawCentralPattern(Color[] pixels, int size, float centerX, float centerY, Color gold, Color darkGold, Color black)
        {
            float patternRadius = size * 0.25f;
            
            // Draw stylized geometric center symbol
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centerX;
                    float dy = y - centerY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (distance < patternRadius)
                    {
                        // Create geometric diamond/star pattern
                        float angle = Mathf.Atan2(dy, dx);
                        float normalizedDist = distance / patternRadius;
                        
                        // Diamond pattern with cel-shaded bands
                        float diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
                        
                        if (diamond < patternRadius * 0.3f)
                        {
                            // Center - dark gold
                            int index = y * size + x;
                            if (index >= 0 && index < pixels.Length)
                            {
                                pixels[index] = darkGold;
                            }
                        }
                        else if (diamond < patternRadius * 0.6f)
                        {
                            // Middle band - gold
                            int index = y * size + x;
                            if (index >= 0 && index < pixels.Length)
                            {
                                pixels[index] = gold;
                            }
                        }
                        
                        // Hard edge outlines (cel-shaded)
                        if (Mathf.Abs(diamond - patternRadius * 0.3f) < 2f ||
                            Mathf.Abs(diamond - patternRadius * 0.6f) < 2f)
                        {
                            int index = y * size + x;
                            if (index >= 0 && index < pixels.Length)
                            {
                                pixels[index] = black;
                            }
                        }
                    }
                }
            }
        }
        
        private void DrawSideBars(Color[] pixels, int size, Color accent, Color outline)
        {
            float barWidth = size * 0.04f;
            float barHeight = size * 0.6f;
            float barY = size * 0.2f;
            
            // Left bar
            for (int y = Mathf.RoundToInt(barY); y < Mathf.RoundToInt(barY + barHeight) && y < size; y++)
            {
                for (int x = Mathf.RoundToInt(size * 0.15f); x < Mathf.RoundToInt(size * 0.15f + barWidth) && x < size; x++)
                {
                    // Outline
                    if (x == Mathf.RoundToInt(size * 0.15f) || x == Mathf.RoundToInt(size * 0.15f + barWidth) - 1 ||
                        y == Mathf.RoundToInt(barY) || y == Mathf.RoundToInt(barY + barHeight) - 1)
                    {
                        int index = y * size + x;
                        if (index >= 0 && index < pixels.Length)
                        {
                            pixels[index] = outline;
                        }
                    }
                    else
                    {
                        int index = y * size + x;
                        if (index >= 0 && index < pixels.Length)
                        {
                            pixels[index] = accent;
                        }
                    }
                }
            }
            
            // Right bar
            for (int y = Mathf.RoundToInt(barY); y < Mathf.RoundToInt(barY + barHeight) && y < size; y++)
            {
                for (int x = Mathf.RoundToInt(size * 0.85f - barWidth); x < Mathf.RoundToInt(size * 0.85f) && x < size; x++)
                {
                    // Outline
                    if (x == Mathf.RoundToInt(size * 0.85f - barWidth) || x == Mathf.RoundToInt(size * 0.85f) - 1 ||
                        y == Mathf.RoundToInt(barY) || y == Mathf.RoundToInt(barY + barHeight) - 1)
                    {
                        int index = y * size + x;
                        if (index >= 0 && index < pixels.Length)
                        {
                            pixels[index] = outline;
                        }
                    }
                    else
                    {
                        int index = y * size + x;
                        if (index >= 0 && index < pixels.Length)
                        {
                            pixels[index] = accent;
                        }
                    }
                }
            }
        }
    }
}
#endif

