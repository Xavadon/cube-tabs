using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor.SkinMaterialGenerator
{
    public class SkinMaterialGeneratorWindow : EditorWindow
    {
        private const string BaseMapProperty = "_BaseMap";

        [SerializeField]
        private Material _baseMaterial;

        [SerializeField]
        private string _outputPath = "Assets/MinecraftModels/Materials";

        [SerializeField]
        private List<Texture2D> _sprites = new();

        private Vector2 _scrollPosition;
        private SerializedObject _serializedObject;
        private SerializedProperty _spritesProperty;

        [MenuItem("Tools/Skin Material Generator")]
        private static void ShowWindow()
        {
            var window = GetWindow<SkinMaterialGeneratorWindow>("Skin Material Generator");
            window.minSize = new Vector2(350, 300);
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _spritesProperty = _serializedObject.FindProperty("_sprites");
        }

        private void OnGUI()
        {
            _serializedObject.Update();

            EditorGUILayout.Space(8);
            _baseMaterial = (Material)EditorGUILayout.ObjectField(
                "Base Material", _baseMaterial, typeof(Material), false);

            EditorGUILayout.Space(4);
            DrawOutputPathField();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.PropertyField(_spritesProperty, GUIContent.none, true);
            EditorGUILayout.EndScrollView();

            _serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            DrawGenerateButton();
        }

        private void DrawOutputPathField()
        {
            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", _outputPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    int assetsIndex = selected.IndexOf("Assets");
                    if (assetsIndex >= 0)
                        _outputPath = selected.Substring(assetsIndex);
                    else
                        Debug.LogError("Output path must be inside the Assets folder.");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGenerateButton()
        {
            bool valid = _baseMaterial != null
                         && !string.IsNullOrEmpty(_outputPath)
                         && _sprites.Count > 0;

            EditorGUI.BeginDisabledGroup(!valid);

            if (GUILayout.Button("Generate Materials", GUILayout.Height(32)))
                GenerateMaterials();

            EditorGUI.EndDisabledGroup();

            if (_baseMaterial == null)
                EditorGUILayout.HelpBox("Assign a Base Material.", MessageType.Warning);
        }

        private void GenerateMaterials()
        {
            if (!AssetDatabase.IsValidFolder(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
                AssetDatabase.Refresh();
            }

            int created = 0;
            int skipped = 0;

            foreach (Texture2D sprite in _sprites)
            {
                if (sprite == null)
                    continue;

                string materialName = sprite.name;
                string assetPath = $"{_outputPath}/{materialName}.mat";

                if (AssetDatabase.LoadAssetAtPath<Material>(assetPath) != null)
                {
                    Debug.LogWarning($"[SkinMaterialGenerator] Skipped '{materialName}' — material already exists.");
                    skipped++;
                    continue;
                }

                Material newMaterial = new Material(_baseMaterial) { name = materialName };
                newMaterial.SetTexture(BaseMapProperty, sprite);

                AssetDatabase.CreateAsset(newMaterial, assetPath);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SkinMaterialGenerator] Done: {created} created, {skipped} skipped.");
        }
    }
}
