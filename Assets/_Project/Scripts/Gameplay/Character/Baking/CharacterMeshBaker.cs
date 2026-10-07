using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Baking
{
    public interface ICharacterMeshBaker : IService
    {
        void Bake(Transform modelRoot, TierData tier);
    }

    public class CharacterMeshBaker : ICharacterMeshBaker
    {
        private const string BakedObjectName = "Baked Mesh";

        private readonly Dictionary<TierData, BakedCharacterMesh> _cache = new();
        private readonly List<MeshRenderer> _renderers = new();
        private readonly CharacterMeshBuilder _builder = new();

        public UniTask Initialize()
        {
            Debug.Log("[CharacterMeshBaker] Initialized");
            return UniTask.CompletedTask;
        }

        public void Bake(Transform modelRoot, TierData tier)
        {
            CollectRenderers(modelRoot);

            if (_renderers.Count == 0)
            {
                return;
            }

            if (!_cache.TryGetValue(tier, out BakedCharacterMesh baked))
            {
                baked = _builder.Build(modelRoot.root.name, modelRoot, _renderers);

                if (baked == null)
                {
                    return;
                }

                _cache.Add(tier, baked);
            }

            if (baked.BoneCount != _renderers.Count)
            {
                Debug.LogError($"[CharacterMeshBaker] '{modelRoot.root.name}' has {_renderers.Count} parts, baked mesh expects {baked.BoneCount}");
                return;
            }

            CreateSkinnedRenderer(modelRoot, baked);

            foreach (MeshRenderer renderer in _renderers)
            {
                renderer.enabled = false;
            }
        }

        public void Dispose()
        {
            foreach (BakedCharacterMesh baked in _cache.Values)
            {
                baked.Release();
            }

            _cache.Clear();
        }

        private void CollectRenderers(Transform modelRoot)
        {
            modelRoot.GetComponentsInChildren(false, _renderers);

            for (int i = _renderers.Count - 1; i >= 0; i--)
            {
                if (!_renderers[i].enabled)
                {
                    _renderers.RemoveAt(i);
                }
            }
        }

        private void CreateSkinnedRenderer(Transform modelRoot, BakedCharacterMesh baked)
        {
            var bakedObject = new GameObject(BakedObjectName);
            bakedObject.layer = modelRoot.gameObject.layer;
            bakedObject.transform.SetParent(modelRoot, false);

            var bones = new Transform[_renderers.Count];

            for (int i = 0; i < _renderers.Count; i++)
            {
                bones[i] = _renderers[i].transform;
            }

            MeshRenderer source = _renderers[0];
            var skinned = bakedObject.AddComponent<SkinnedMeshRenderer>();
            skinned.quality = SkinQuality.Bone1;
            skinned.sharedMesh = baked.Mesh;
            skinned.bones = bones;
            skinned.sharedMaterials = baked.Materials;
            skinned.localBounds = baked.Bounds;
            skinned.shadowCastingMode = source.shadowCastingMode;
            skinned.receiveShadows = source.receiveShadows;
        }
    }
}
