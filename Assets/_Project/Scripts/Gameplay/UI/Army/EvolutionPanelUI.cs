using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Shop;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class EvolutionPanelUI : MonoBehaviour
    {
        [SerializeField]
        private EvolutionOptionUI _optionPrefab;

        [SerializeField]
        private Transform _optionsContainer;

        private IPlayerProgressService _progress;
        private EvolutionCatalog _evolutionCatalog;
        private UnitPreviewFactory _portraitFactory;
        private Dictionary<int, RenderTexture> _portraitCache;

        public void Initialize(IPlayerProgressService progress, EvolutionCatalog evolutionCatalog,
            UnitPreviewFactory portraitFactory, Dictionary<int, RenderTexture> portraitCache)
        {
            _progress = progress;
            _evolutionCatalog = evolutionCatalog;
            _portraitFactory = portraitFactory;
            _portraitCache = portraitCache;
        }

        public void Show(int instanceId, CharacterData currentData, int tierIndex)
        {
            Clear();

            bool canUpgradeTier = tierIndex < currentData.MaxTier;

            if (canUpgradeTier)
            {
                ShowTierUpgrade(instanceId, currentData, tierIndex);
            }
            else
            {
                ShowEvolutions(instanceId, currentData);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Clear();
        }

        private void ShowTierUpgrade(int instanceId, CharacterData data, int tierIndex)
        {
            var nextTier = data.GetTier(tierIndex + 1);
            if (nextTier == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            int cost = nextTier.EvolutionCost;
            var portrait = GetOrCreatePortrait(data, tierIndex + 1);

            var optionUI = Instantiate(_optionPrefab, _optionsContainer);
            int capturedId = instanceId;

            optionUI.Init(
                $"{data.Name} T{tierIndex + 2}",
                cost,
                _progress.CanAfford(cost),
                portrait,
                () => _progress.UpgradeTier(capturedId));
        }

        private void ShowEvolutions(int instanceId, CharacterData currentData)
        {
            var options = _evolutionCatalog.GetEvolutions(currentData.Id);

            if (options.Length == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            foreach (var option in options)
            {
                var optionUI = Instantiate(_optionPrefab, _optionsContainer);
                int capturedId = instanceId;
                var capturedTarget = option.Target;
                int capturedCost = option.Cost;

                var portrait = GetOrCreatePortrait(option.Target, 0);

                optionUI.Init(
                    option.Target.Name,
                    capturedCost,
                    _progress.CanAfford(capturedCost),
                    portrait,
                    () => _progress.EvolveUnit(capturedId, capturedTarget, capturedCost));
            }
        }

        private RenderTexture GetOrCreatePortrait(CharacterData data, int tierIndex)
        {
            int key = data.Id * 100 + tierIndex;

            if (_portraitCache.TryGetValue(key, out var existing))
                return existing;

            var handle = _portraitFactory.CreatePreview(data, tierIndex, _portraitCache.Count);
            _portraitCache[key] = handle.Texture;
            return handle.Texture;
        }

        private void Clear()
        {
            for (int i = _optionsContainer.childCount - 1; i >= 0; i--)
                Destroy(_optionsContainer.GetChild(i).gameObject);
        }
    }
}
