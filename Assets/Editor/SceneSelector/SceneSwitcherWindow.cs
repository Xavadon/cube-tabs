#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditorSceneSwitcher
{
    [MenuItem("SceneSwitcher/AwaitInit ^&0", false, 0)]
    private static void SwitchToAwaitInit() => SwitchToScene(0);

    [MenuItem("SceneSwitcher/Boot ^&1", false, 1)]
    private static void SwitchToBoot() => SwitchToScene(1);

    [MenuItem("SceneSwitcher/Menu ^&2", false, 2)]
    private static void SwitchToMenu() => SwitchToScene(2);

    [MenuItem("SceneSwitcher/Goblin Cave ^&3", false, 3)]
    private static void SwitchToGoblinCave() => SwitchToScene(3);

    [MenuItem("SceneSwitcher/Frog Swamp ^&4", false, 4)]
    private static void SwitchToFrogSwamp() => SwitchToScene(4);

    [MenuItem("SceneSwitcher/Zombie Village ^&5", false, 5)]
    private static void SwitchToZombieVillage() => SwitchToScene(5);

    [MenuItem("SceneSwitcher/Dark Territory ^&6", false, 6)]
    private static void SwitchToDarkTerritory() => SwitchToScene(6);

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
