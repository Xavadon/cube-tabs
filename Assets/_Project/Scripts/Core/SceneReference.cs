using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    [Serializable]
    public class SceneReference
    {
        [SerializeField]
        private string _sceneName;

#if UNITY_EDITOR
        [SerializeField]
        private UnityEditor.SceneAsset _sceneAsset;
#endif

        public string SceneName => _sceneName;

#if UNITY_EDITOR
        public void OnValidate()
        {
            _sceneName = _sceneAsset != null ? _sceneAsset.name : string.Empty;
        }
#endif
    }
}
