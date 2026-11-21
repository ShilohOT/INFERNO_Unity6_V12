#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class CreateInfernoScene
{
    static CreateInfernoScene() { EditorApplication.delayCall += EnsureScene; }

    [MenuItem("Inferno / Crear o regenerar escena de demo")]
    public static void RebuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorUtility.DisplayDialog("Regenerar escena", "Se reemplazara la escena generada. No afecta tus scripts o imagenes.", "Continuar", "Cancelar")) return;
        Generate();
    }

    static void EnsureScene()
    {
        ConfigureSprites();
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists("Assets/Scenes/InfernoDemo.unity")) return;
        Generate();
    }

    static void ConfigureSprites()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Art" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }

    static void Generate()
    {
        ConfigureSprites();
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var obj = new GameObject("INFERNO - Demo principal");
        obj.AddComponent<InfernoDemo>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/InfernoDemo.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/InfernoDemo.unity", true) };
        EditorSceneManager.OpenScene("Assets/Scenes/InfernoDemo.unity");
        Debug.Log("INFERNO: escena creada. Pulsa Play para probar la demo.");
    }
}
#endif
