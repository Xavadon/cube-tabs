using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
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
        private IUnitPreviewService _previewService;

        public void Initialize(IPlayerProgressService progress, EvolutionCatalog evolutionCatalog,
            IUnitPreviewService previewService)
        {
            _progress = progress;
            _evolutionCatalog = evolutionCatalog;
            _previewService = previewService;
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
            var preview = _previewService.GetFullBody(data, tierIndex + 1);

            var optionUI = Instantiate(_optionPrefab, _optionsContainer);
            int capturedId = instanceId;

            optionUI.Init(
                $"{data.Name} T{tierIndex + 2}",
                cost,
                _progress.CanAfford(cost),
                preview,
                nextTier.Stats.Health,
                nextTier.Stats.Damage,
                nextTier.MoveSpeed,
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

                var preview = _previewService.GetFullBody(option.Target, 0);
                var tier = option.Target.GetTier(0);

                optionUI.Init(
                    option.Target.Name,
                    capturedCost,
                    _progress.CanAfford(capturedCost),
                    preview,
                    tier.Stats.Health,
                    tier.Stats.Damage,
                    tier.MoveSpeed,
                    () => _progress.EvolveUnit(capturedId, capturedTarget, capturedCost));
            }
        }

        private void Clear()
        {
            for (int i = _optionsContainer.childCount - 1; i >= 0; i--)
                Destroy(_optionsContainer.GetChild(i).gameObject);
        }
    }
}
