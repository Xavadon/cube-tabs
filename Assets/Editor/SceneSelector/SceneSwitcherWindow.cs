#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditorSceneSwitcher
{
    [MenuItem("SceneSwitcher/Boot ^&0", false, 0)]
    private static void SwitchToBoot() => SwitchToScene(0);

    [MenuItem("SceneSwitcher/Menu ^&1", false, 1)]
    private static void SwitchToMenu() => SwitchToScene(1);

    [MenuItem("SceneSwitcher/Goblin Cave ^&2", false, 2)]
    private static void SwitchToGoblinCave() => SwitchToScene(2);

    [MenuItem("SceneSwitcher/Frog Swamp ^&3", false, 3)]
    private static void SwitchToFrogSwamp() => SwitchToScene(3);

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
