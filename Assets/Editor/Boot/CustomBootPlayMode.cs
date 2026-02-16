#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Core.Utils.Editor
{
    public class CustomBootPlayMode
    {
        [InitializeOnLoadMethod]
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