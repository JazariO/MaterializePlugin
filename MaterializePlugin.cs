using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class MaterializePlugin : EditorWindow
{
    [MenuItem("Assets/Materialize", false, 20)]
    static void Materialize()
    {
        string[] guids = Selection.assetGUIDs;
        string[] assetPaths = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
        }
        ProcessTextures(assetPaths);
    }

    [MenuItem("Assets/Materialize", true)]
    static bool ValidateMaterialize()
    {
        return Selection.objects.Length > 0 && Selection.objects.All(obj => obj is Texture);
    }

    static void ProcessTextures(string[] texturePaths)
    {
        if (texturePaths.Length == 0)
            return;

        string baseName = GetBaseName(texturePaths[0]);
        string[] nameParts = baseName.Split('_');
        string category = nameParts[0];
        string outputPath = $"Assets/Materials/{category}/{baseName}.mat";

        // Create the directory if it doesn't exist
        string directoryPath = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
        }

        // Create a new material with the specified shader
        Shader shader = Shader.Find("Shader Graphs/s_Materialize");
        if (shader == null)
        {
            Debug.LogError("Shader 'Shader Graphs/s_Materialize' not found!");
            return;
        }

        Material material = new Material(shader);

        foreach (string texturePath in texturePaths)
        {
            string textureName = Path.GetFileNameWithoutExtension(texturePath);

            // Modify texture import settings
            ModifyTextureImportSettings(texturePath, textureName);

            // Reimport the texture to apply the new settings
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);

            if (textureName.Contains("Diffuse"))
            {
                material.SetTexture("_Diffuse_Map", texture);
            } else if (textureName.Contains("MAOS"))
            {
                material.SetTexture("_MAOS_Map", texture);
            } else if (textureName.Contains("NormalGL"))
            {
                material.SetTexture("_Normal_Map", texture);
            }
        }

        // Save the material asset
        try
        {
            AssetDatabase.CreateAsset(material, outputPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Material created and saved at: {outputPath}");
        } catch (System.Exception e)
        {
            Debug.LogError($"Failed to create material at {outputPath}. Error: {e.Message}");
        }
    }

    static void ModifyTextureImportSettings(string texturePath, string textureName)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            if (textureName.Contains("Diffuse") || textureName.Contains("MAOS"))
            {
                importer.alphaSource = TextureImporterAlphaSource.None;
            }

            if (textureName.Contains("MAOS"))
            {
                importer.sRGBTexture = false;
            }

            if (textureName.Contains("NormalGL"))
            {
                importer.textureType = TextureImporterType.NormalMap;
            }

            importer.SaveAndReimport();
        }
    }

    static string GetBaseName(string path)
    {
        string fileName = Path.GetFileNameWithoutExtension(path);
        string[] parts = fileName.Split('_');
        return string.Join("_", parts.Take(2));
    }
}