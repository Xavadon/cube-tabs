#if UNITY_EDITOR
using _Project.Scripts.Dev;
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

        private static void SetPlayModeStartScene()
        {
            if (Object.FindAnyObjectByType<SandboxBootstrap>() != null)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

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