using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.UI.Shop;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services
{
    public interface IUnitPreviewService : IService
    {
        Quaternion DefaultFullBodyRotation { get; }
        RenderTexture GetPortrait(CharacterData data, int tierIndex);
        PreviewHandle GetFullBody(CharacterData data, int tierIndex);
    }

    public class UnitPreviewService : IUnitPreviewService
    {
        private const string PortraitConfigPath = "Data/UnitPreviewConfig";
        private const string FullBodyConfigPath = "Data/UnitPreviewConfig_FullBody";

        private UnitPreviewFactory _portraitFactory;
        private UnitPreviewFactory _fullBodyFactory;
        private readonly Dictionary<int, RenderTexture> _portraitCache = new();
        private readonly Dictionary<int, PreviewHandle> _fullBodyCache = new();

        public Quaternion DefaultFullBodyRotation { get; private set; }

        public UniTask Initialize()
        {
            var portraitConfig = Resources.Load<UnitPreviewConfig>(PortraitConfigPath);
            var fullBodyConfig = Resources.Load<UnitPreviewConfig>(FullBodyConfigPath);

            _portraitFactory = new UnitPreviewFactory(portraitConfig);
            _fullBodyFactory = new UnitPreviewFactory(fullBodyConfig);
            DefaultFullBodyRotation = Quaternion.Euler(fullBodyConfig.ModelRotation);

            return UniTask.CompletedTask;
        }

        public RenderTexture GetPortrait(CharacterData data, int tierIndex)
        {
            int key = HashUnitTier(data.Id);

            //Debug.Log($"[UnitPreviewService] Try get portrait: [Name: <color=\"green\">{data.Name}</color> key: {key}]");

            if (_portraitCache.TryGetValue(key, out var existing))
            {
                //Debug.Log($"[UnitPreviewService] Success get existing portrait: [Name: <color=\"green\">{data.Name}</color> key: {key}]");
                return existing;
            }

            //Debug.Log($"[UnitPreviewService] Failed get existing portrait, create: [Name: <color=\"green\">{data.Name}</color> key: {key}]");
            var handle = _portraitFactory.CreatePreview(data, tierIndex, _portraitCache.Count);
            _portraitCache[key] = handle.Texture;
            return handle.Texture;
        }

        public PreviewHandle GetFullBody(CharacterData data, int tierIndex)
        {
            int key = HashUnitTier(data.Id);

            if (_fullBodyCache.TryGetValue(key, out var existing))
                return existing;

            var handle = _fullBodyFactory.CreatePreview(data, tierIndex, _fullBodyCache.Count);
            _fullBodyCache[key] = handle;
            return handle;
        }

        public void Dispose()
        {
            _portraitFactory?.Dispose();
            _fullBodyFactory?.Dispose();
            _portraitCache.Clear();
            _fullBodyCache.Clear();
        }

        private static int HashUnitTier(int unitId)
        {
            return unitId * 100;
        }
    }
}
