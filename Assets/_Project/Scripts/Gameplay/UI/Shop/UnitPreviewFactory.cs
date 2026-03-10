using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Gameplay.UI.Shop
{
    public readonly struct PreviewHandle
    {
        public readonly RenderTexture Texture;
        public readonly Transform Model;

        public PreviewHandle(RenderTexture texture, Transform model)
        {
            Texture = texture;
            Model = model;
        }
    }

    public class UnitPreviewFactory
    {
        private const string CharacterPrefabPath = "Prefab/DefaultCharacter";
        private const int TextureDepth = 16;

        private readonly UnitPreviewConfig _config;
        private readonly List<PreviewRoom> _rooms = new();
        private GameObject _characterPrefab;

        public UnitPreviewFactory(UnitPreviewConfig config)
        {
            _config = config;
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
            ApplyVisualsAndStrip(modelGO, tier);

            var rt = new RenderTexture(_config.TextureSize, _config.TextureSize, TextureDepth);

            CreateCamera(root.transform, roomPos, rt);

            modelGO.transform.rotation = Quaternion.Euler(_config.ModelRotation);

            CreateLight(root.transform, roomPos);

            _rooms.Add(new PreviewRoom(root, rt));
            return new PreviewHandle(rt, modelGO.transform);
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

        private static void ApplyVisualsAndStrip(GameObject go, TierData tier)
        {
            if (go.TryGetComponent(out Character.Character character))
            {
                character.SkinChanger.ChangeSkin(tier.SkinMaterial);
                character.ArmorChanger.ChangeSkin(tier.ArmorMaterial != null ? tier.ArmorMaterial : tier.SkinMaterial);
                character.enabled = false;
                Object.Destroy(character);
            }

            if (tier.WeaponData is { Length: > 0 })
            {
                var weaponChanger = go.GetComponentInChildren<WeaponChanger>();
                if (weaponChanger != null)
                    weaponChanger.SetWeapon(tier.WeaponData[0]);
            }

            if (go.TryGetComponent(out NavMeshAgent agent))
            {
                agent.enabled = false;
                Object.Destroy(agent);
            }

            foreach (var collider in go.GetComponentsInChildren<Collider>())
                Object.Destroy(collider);
        }

        private void CreateCamera(Transform parent, Vector3 roomPos, RenderTexture rt)
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
