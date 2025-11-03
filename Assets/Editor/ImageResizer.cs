using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;

public class ImageResizer : EditorWindow
{
    private string folderPath = "Assets";
    private bool includeSubfolders = true;
    private Vector2 scrollPos;
    private List<string> processedFiles = new List<string>();
    private bool isProcessing = false;

    [MenuItem("Tools/Image Resizer")]
    public static void ShowWindow()
    {
        GetWindow<ImageResizer>("Image Resizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("PNG Resizer to 2048px Width", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        GUILayout.Label("Folder Path:");
        folderPath = EditorGUILayout.TextField(folderPath);
        
        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Select Folder"))
        {
            folderPath = EditorUtility.OpenFolderPanel("Select Folder", folderPath, "");
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = "Assets";
            }
        }
        
        EditorGUILayout.Space();
        
        GUI.enabled = !isProcessing && Directory.Exists(folderPath);
        
        if (GUILayout.Button("Process PNG Files", GUILayout.Height(30)))
        {
            ProcessImages();
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        if (isProcessing)
        {
            EditorGUILayout.HelpBox("Processing images... Please wait.", MessageType.Info);
        }
        
        if (processedFiles.Count > 0)
        {
            GUILayout.Label($"Processed {processedFiles.Count} files:", EditorStyles.boldLabel);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            
            foreach (string file in processedFiles)
            {
                EditorGUILayout.LabelField(file);
            }
            
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Clear List"))
            {
                processedFiles.Clear();
            }
        }
    }

    private async void ProcessImages()
    {
        isProcessing = true;
        processedFiles.Clear();
        Repaint();

        try
        {
            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] pngFiles = Directory.GetFiles(folderPath, "*.png", searchOption);
            
            int targetWidth = 2048;
            int processedCount = 0;

            foreach (string filePath in pngFiles)
            {
                if (await ResizeImage(filePath, targetWidth))
                {
                    processedFiles.Add(Path.GetFileName(filePath));
                    processedCount++;
                    
                    // Update progress bar
                    float progress = (float)processedCount / pngFiles.Length;
                    EditorUtility.DisplayProgressBar("Processing Images", 
                        $"Processing {Path.GetFileName(filePath)}...", progress);
                }
            }

            EditorUtility.ClearProgressBar();
            Debug.Log($"Successfully processed {processedCount} PNG files.");
            
            // Refresh asset database if processing Assets folder
            if (folderPath.StartsWith(Application.dataPath))
            {
                AssetDatabase.Refresh();
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"Error processing images: {e.Message}");
        }
        finally
        {
            isProcessing = false;
            Repaint();
        }
    }

    private async Task<bool> ResizeImage(string filePath, int targetWidth)
    {
        try
        {
            // Read the PNG file
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D texture = new Texture2D(2, 2);
            
            if (!texture.LoadImage(fileData))
            {
                Debug.LogWarning($"Failed to load image: {filePath}");
                return false;
            }

            int originalWidth = texture.width;
            int originalHeight = texture.height;
            
            // Calculate new height maintaining aspect ratio
            float aspectRatio = (float)originalHeight / originalWidth;
            int targetHeight = Mathf.RoundToInt(targetWidth * aspectRatio);
            
            // Skip if already at target size
            if (originalWidth == targetWidth && originalHeight == targetHeight)
            {
                Debug.Log($"Image {Path.GetFileName(filePath)} already at target size");
                return true;
            }

            // Create render texture for resizing
            RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 24);
            RenderTexture.active = rt;

            // Create temporary texture for blit operation
            Texture2D tempTexture = new Texture2D(originalWidth, originalHeight, TextureFormat.RGBA32, false);
            tempTexture.SetPixels(texture.GetPixels());
            tempTexture.Apply();

            // Blit to resize
            Graphics.Blit(tempTexture, rt);

            // Read resized texture
            Texture2D resultTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            resultTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            resultTexture.Apply();

            // Encode to PNG
            byte[] pngData = resultTexture.EncodeToPNG();

            // Cleanup
            RenderTexture.active = null;
            DestroyImmediate(rt);
            DestroyImmediate(texture);
            DestroyImmediate(tempTexture);
            DestroyImmediate(resultTexture);

            // Write file
            File.WriteAllBytes(filePath, pngData);
            
            Debug.Log($"Resized {Path.GetFileName(filePath)}: {originalWidth}x{originalHeight} -> {targetWidth}x{targetHeight}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error resizing {filePath}: {e.Message}");
            return false;
        }
    }
}