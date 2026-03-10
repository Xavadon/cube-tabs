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

        public void Show(int instanceId, CharacterData currentData)
        {
            Clear();

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
                int capturedIndex = instanceId;
                var capturedTarget = option.Target;
                int capturedCost = option.Cost;

                var portrait = GetOrCreatePortrait(option.Target);

                optionUI.Init(
                    option.Target.Name,
                    option.Cost,
                    _progress.CanAfford(option.Cost),
                    portrait,
                    () => _progress.EvolveUnit(capturedIndex, capturedTarget, capturedCost));
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Clear();
        }

        private RenderTexture GetOrCreatePortrait(CharacterData data)
        {
            if (_portraitCache.TryGetValue(data.Id, out var existing))
                return existing;

            var handle = _portraitFactory.CreatePreview(data, 0, _portraitCache.Count);
            _portraitCache[data.Id] = handle.Texture;
            return handle.Texture;
        }

        private void Clear()
        {
            for (int i = _optionsContainer.childCount - 1; i >= 0; i--)
                Destroy(_optionsContainer.GetChild(i).gameObject);
        }
    }
}
