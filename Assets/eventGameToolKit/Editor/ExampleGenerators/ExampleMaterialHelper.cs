using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Helper utility for example generators to load or create materials
/// Ensures examples always work even if materials are missing
/// </summary>
public static class ExampleMaterialHelper
{
    private const string MATERIALS_FOLDER = "Assets/Materials";
    private const string PINK_MAT_PATH = "Assets/Materials/pink.mat";
    private const string BLUE_MAT_PATH = "Assets/Materials/blue.mat";

    /// <summary>
    /// Get or create the pink material used by examples
    /// </summary>
    public static Material GetPinkMaterial()
    {
        return GetOrCreateMaterial(PINK_MAT_PATH, new Color(1f, 0.4f, 0.7f), "Pink");
    }

    /// <summary>
    /// Get or create the blue material used by examples
    /// </summary>
    public static Material GetBlueMaterial()
    {
        return GetOrCreateMaterial(BLUE_MAT_PATH, new Color(0.3f, 0.6f, 1f), "Blue");
    }

    /// <summary>
    /// Get or create a material with the specified name and color (public API for setup generators)
    /// </summary>
    public static Material GetOrCreateMaterial(string materialName, Color color)
    {
        string path = $"{MATERIALS_FOLDER}/{materialName.ToLower()}.mat";
        return GetOrCreateMaterial(path, color, materialName);
    }

    /// <summary>
    /// Get or create a material at the specified path with the given color
    /// </summary>
    private static Material GetOrCreateMaterial(string path, Color color, string materialName)
    {
        // Try to load existing material
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
        {
            return mat;
        }

        // Material doesn't exist, create it
        Debug.LogWarning($"Example Generator: Material not found at {path}. Creating default {materialName} material.");

        // Ensure Materials folder exists
        if (!AssetDatabase.IsValidFolder(MATERIALS_FOLDER))
        {
            string parentFolder = "Assets";
            string folderName = "Materials";
            AssetDatabase.CreateFolder(parentFolder, folderName);
            Debug.Log($"Created Materials folder at {MATERIALS_FOLDER}");
        }

        // Create material with URP/Lit shader
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.name = materialName.ToLower();

        // Save material to Assets
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created {materialName} material at {path}");

        return mat;
    }

    /// <summary>
    /// Create a temporary material with specified color (not saved to Assets)
    /// Useful for one-off objects that don't need persistent materials
    /// </summary>
    public static Material CreateTemporaryMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        return mat;
    }

    /// <summary>
    /// Create a gray material for ground/environment objects
    /// </summary>
    public static Material CreateGrayMaterial()
    {
        return CreateTemporaryMaterial(new Color(0.3f, 0.3f, 0.3f));
    }

    /// <summary>
    /// Create a yellow material for indicators
    /// </summary>
    public static Material CreateYellowMaterial()
    {
        return CreateTemporaryMaterial(Color.yellow);
    }

    /// <summary>
    /// Validate that the URP Lit shader is available
    /// </summary>
    public static bool ValidateURPShader()
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");

        if (urpShader == null)
        {
            Debug.LogError("Example Generator: Could not find 'Universal Render Pipeline/Lit' shader! Is URP installed?");
            return false;
        }

        return true;
    }
}
