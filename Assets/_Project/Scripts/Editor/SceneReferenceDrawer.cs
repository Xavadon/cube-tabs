using _Project.Scripts.Core;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor
{
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var sceneAssetProperty = property.FindPropertyRelative("_sceneAsset");
            var sceneNameProperty = property.FindPropertyRelative("_sceneName");

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, sceneAssetProperty, label);

            if (EditorGUI.EndChangeCheck())
            {
                var sceneAsset = sceneAssetProperty.objectReferenceValue as SceneAsset;
                sceneNameProperty.stringValue = sceneAsset != null ? sceneAsset.name : string.Empty;
            }

            EditorGUI.EndProperty();
        }
    }
}
