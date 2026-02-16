#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditorSceneSwitcher
{
    [MenuItem("SceneSwitcher/Boot ^&0", false, 0)]
    private static void SwitchToBoot() => SwitchToScene(0);

    [MenuItem("SceneSwitcher/Game ^&1", false, 1)]
    private static void SwitchToGame() => SwitchToScene(1);

    private static void SwitchToScene(int index)
    {
        if (index >= 0 && index < EditorBuildSettings.scenes.Length)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                var scene = EditorBuildSettings.scenes[index];
                EditorSceneManager.OpenScene(scene.path);
                Debug.Log($"Switched to scene: {scene.path}");
            }
        }
        else Debug.LogError("Scene index out of range.");
    }
}
#endif
