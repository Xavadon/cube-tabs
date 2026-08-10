#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Core.Utils.Editor
{
    [InitializeOnLoad]
    public class CustomBootPlayMode
    {
        static CustomBootPlayMode()
        {
            SetPlayModeStartScene();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                SetPlayModeStartScene();
        }

        // Play с любой сцены всегда идёт через Boot: как настоящий запуск игры
        private static void SetPlayModeStartScene()
        {
            var path = "Assets/_Project/Scenes/Boot.unity";
            SceneAsset myWantedStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (myWantedStartScene != null)
                EditorSceneManager.playModeStartScene = myWantedStartScene;
            else
                Debug.Log("Could not find Scene " + path);
        }
    }
}

#endif