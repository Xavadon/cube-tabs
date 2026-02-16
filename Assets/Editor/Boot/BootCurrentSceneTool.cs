using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Scripts.Utils.Editor
{
    public static class BootCurrentSceneTool
    {
        [MenuItem("Tools/Boot/Set loading scene To current scene")]
        public static void Current()
        {
            EditorSceneManager.playModeStartScene = null;
        }

        [MenuItem("Tools/Boot/Set loading scene To boot")]
        public static void Boot()
        {
            var path = "Assets/Game/Scenes/BootStrap.unity";
            SceneAsset myWantedStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (myWantedStartScene != null)
                EditorSceneManager.playModeStartScene = myWantedStartScene;
            else
                Debug.Log("Could not find Scene " + path);
        }
    }
}