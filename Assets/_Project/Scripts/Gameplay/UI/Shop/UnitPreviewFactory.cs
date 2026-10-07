using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Baking;
using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public readonly struct PreviewHandle
    {
        public readonly RenderTexture Texture;
        public readonly Transform Model;

        private readonly GameObject _room;

        public PreviewHandle(RenderTexture texture, Transform model, GameObject room)
        {
            Texture = texture;
            Model = model;
            _room = room;
        }

        public void SetActive(bool active)
        {
            if (_room != null)
            {
                _room.SetActive(active);
            }
        }
    }

    public class UnitPreviewFactory
    {
        private const string CharacterPrefabPath = "Prefab/DefaultCharacter";
        private const int TextureDepth = 16;

        private readonly UnitPreviewConfig _config;
        private readonly ICharacterMeshBaker _meshBaker;
        private readonly List<PreviewRoom> _rooms = new();
        private GameObject _characterPrefab;

        public UnitPreviewFactory(UnitPreviewConfig config, ICharacterMeshBaker meshBaker)
        {
            _config = config;
            _meshBaker = meshBaker;
        }

        public PreviewHandle CreatePreview(CharacterData data, int tierIndex, int roomIndex)
        {
            LoadPrefabIfNeeded();

            var tier = data.GetTier(tierIndex);
            if (tier == null)
                return default;

            Vector3 roomPos = _config.RoomOrigin + Vector3.right * (roomIndex * _config.RoomSpacing);

            var root = new GameObject($"PreviewRoom_{data.Name}");
            root.transform.position = roomPos;

            var modelGO = Object.Instantiate(_characterPrefab, roomPos, Quaternion.identity, root.transform);
            ApplyVisualsAndStrip(modelGO, data, tier);

            var rt = new RenderTexture(_config.TextureSize, _config.TextureSize, TextureDepth);

            Camera camera = CreateCamera(root.transform, roomPos, rt);

            modelGO.transform.rotation = Quaternion.Euler(_config.ModelRotation);

            CreateLight(root.transform, roomPos);

            if (_config.RenderOnce)
            {
                RenderSnapshot(modelGO, camera);
            }

            root.SetActive(false);
            _rooms.Add(new PreviewRoom(root, rt));
            return new PreviewHandle(rt, modelGO.transform, root);
        }

        public void Dispose()
        {
            foreach (var room in _rooms)
            {
                if (room.Root != null)
                    Object.Destroy(room.Root);

                if (room.Texture != null)
                    room.Texture.Release();
            }

            _rooms.Clear();
        }

        private void LoadPrefabIfNeeded()
        {
            if (_characterPrefab == null)
                _characterPrefab = Resources.Load<GameObject>(CharacterPrefabPath);
        }

        private void ApplyVisualsAndStrip(GameObject go, CharacterData data, TierData tier)
        {
            if (data.AnimatorOverride != null)
            {
                var animator = go.GetComponentInChildren<Animator>();
                if (animator != null)
                    animator.runtimeAnimatorController = data.AnimatorOverride;
            }

            if (tier.WeaponData is { Length: > 0 })
            {
                var weaponChanger = go.GetComponentInChildren<WeaponChanger>();
                if (weaponChanger != null)
                    for (int i = 0; i < tier.WeaponData.Length; i++)
                        weaponChanger.SetWeapon(tier.WeaponData[i], i);
            }

            if (go.TryGetComponent(out Character.Character character))
            {
                character.SkinChanger.ChangeSkin(tier.SkinMaterial);
                character.ArmorChanger.ChangeSkin(tier.ArmorMaterial != null ? tier.ArmorMaterial : tier.SkinMaterial);
                _meshBaker.Bake(character.ModelRoot, tier);
                character.enabled = false;
                Object.Destroy(character);
            }

            if (go.TryGetComponent(out NavMeshAgent agent))
            {
                agent.enabled = false;
                Object.Destroy(agent);
            }

            foreach (var collider in go.GetComponentsInChildren<Collider>())
                Object.Destroy(collider);
        }

        private static void RenderSnapshot(GameObject model, Camera camera)
        {
            if (model.TryGetComponent(out Animator animator))
            {
                animator.Update(0f);
            }

            camera.Render();
        }

        private Camera CreateCamera(Transform parent, Vector3 roomPos, RenderTexture rt)
        {
            var camGO = new GameObject("PreviewCamera");
            camGO.transform.SetParent(parent);
            camGO.transform.position = roomPos + _config.CameraOffset;
            camGO.transform.LookAt(roomPos + _config.CameraLookOffset);

            var cam = camGO.AddComponent<Camera>();
            cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _config.CameraBackgroundColor;
            cam.nearClipPlane = _config.CameraNear;
            cam.farClipPlane = _config.CameraFar;
            cam.fieldOfView = _config.CameraFov;
            cam.GetUniversalAdditionalCameraData().renderShadows = false;
            return cam;
        }

        private void CreateLight(Transform parent, Vector3 roomPos)
        {
            var lightGO = new GameObject("PreviewLight");
            lightGO.transform.SetParent(parent);
            lightGO.transform.position = roomPos + _config.LightOffset;
            lightGO.transform.LookAt(roomPos + Vector3.up * 0.5f);

            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Spot;
            light.intensity = _config.LightIntensity;
            light.range = _config.LightRange;
            light.spotAngle = _config.LightSpotAngle;
        }

        private readonly struct PreviewRoom
        {
            public readonly GameObject Root;
            public readonly RenderTexture Texture;

            public PreviewRoom(GameObject root, RenderTexture texture)
            {
                Root = root;
                Texture = texture;
            }
        }
    }
}
