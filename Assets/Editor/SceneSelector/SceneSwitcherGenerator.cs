#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class SceneSwitcherGenerator
{
    private const string outputPath = "Assets/Editor/SceneSelector/SceneSwitcherWindow.cs";
    
    [MenuItem("Tools/Generate Scene Switcher")]
    public static void GenerateSceneSwitcher()
    {
        var scenes = EditorBuildSettings.scenes;

        if (scenes.Length == 0)
        {
            Debug.LogWarning("No scenes found in Build Settings.");
            return;
        }

        using (StreamWriter writer = new StreamWriter(outputPath, false))
        {
            writer.WriteLine("#if UNITY_EDITOR");
            writer.WriteLine("using UnityEditor;");
            writer.WriteLine("using UnityEditor.SceneManagement;");
            writer.WriteLine("using UnityEngine;");
            writer.WriteLine();
            writer.WriteLine("public static class EditorSceneSwitcher");
            writer.WriteLine("{");

            for (int i = 0; i < scenes.Length; i++)
            {
                string scenePath = scenes[i].path;
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                string methodName = $"SwitchTo{sceneName.Replace(" ", "")}";
                string hotkey = i < 10 ? $" ^&{i}" : ""; // Ctrl+Alt+0-9 only for first 10
                string menuPath = $"SceneSwitcher/{sceneName}{hotkey}";

                writer.WriteLine($"    [MenuItem(\"{menuPath}\", false, {i})]");
                writer.WriteLine($"    private static void {methodName}() => SwitchToScene({i});");
                writer.WriteLine();
            }

            writer.WriteLine("    private static void SwitchToScene(int index)");
            writer.WriteLine("    {");
            writer.WriteLine("        if (index >= 0 && index < EditorBuildSettings.scenes.Length)");
            writer.WriteLine("        {");
            writer.WriteLine("            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())");
            writer.WriteLine("            {");
            writer.WriteLine("                var scene = EditorBuildSettings.scenes[index];");
            writer.WriteLine("                EditorSceneManager.OpenScene(scene.path);");
            writer.WriteLine("                Debug.Log($\"Switched to scene: {scene.path}\");");
            writer.WriteLine("            }");
            writer.WriteLine("        }");
            writer.WriteLine("        else Debug.LogError(\"Scene index out of range.\");");
            writer.WriteLine("    }");

            writer.WriteLine("}");
            writer.WriteLine("#endif");
        }

        AssetDatabase.Refresh();
        Debug.Log("EditorSceneSwitcher.cs generated!");
    }
}
#endif
