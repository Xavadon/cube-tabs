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

        public void Initialize(IPlayerProgressService progress, EvolutionCatalog evolutionCatalog)
        {
            _progress = progress;
            _evolutionCatalog = evolutionCatalog;
        }

        public void Show(int ownedIndex, CharacterData currentData)
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
                int capturedIndex = ownedIndex;
                var capturedTarget = option.Target;
                int capturedCost = option.Cost;

                optionUI.Init(
                    option.Target.Name,
                    option.Cost,
                    _progress.CanAfford(option.Cost),
                    () => _progress.EvolveUnit(capturedIndex, capturedTarget, capturedCost));
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Clear();
        }

        private void Clear()
        {
            for (int i = _optionsContainer.childCount - 1; i >= 0; i--)
                Destroy(_optionsContainer.GetChild(i).gameObject);
        }
    }
}
