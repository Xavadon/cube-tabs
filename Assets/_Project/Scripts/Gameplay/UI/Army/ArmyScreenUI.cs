using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class ArmyScreenUI : MonoBehaviour
    {
        [SerializeField]
        private ArmyUnitCardUI _cardPrefab;

        [Header("Army")]
        [SerializeField]
        private Transform _armyContainer;

        [FormerlySerializedAs("_backlogContainer")]
        [Header("Reserve")]
        [SerializeField]
        private Transform _reserveContainer;

        [SerializeField]
        private TextMeshProUGUI _slotCountLabel;

        [Header("Buy")]
        [SerializeField]
        private Button _buyButton;

        [SerializeField]
        private TextMeshProUGUI _buyButtonCostLabel;

        [SerializeField]
        private TextMeshProUGUI _goldLabel;

        [Header("Evolution")]
        [SerializeField]
        private EvolutionPanelUI _evolutionPanel;

        private IPlayerProgressService _progress;
        private ShopCatalog _catalog;
        private UnitPreviewFactory _previewFactory;
        private readonly Dictionary<int, RenderTexture> _portraitCache = new();
        private CharacterData _selectedUnit;
        private readonly List<(ArmyUnitCardUI card, CharacterData data)> _activeCards = new();

        public void Initialize(IPlayerProgressService progress, ShopCatalog catalog, UnitPreviewConfig previewConfig)
        {
            _progress = progress;
            _catalog = catalog;
            _previewFactory = new UnitPreviewFactory(previewConfig);

            _evolutionPanel.Initialize(progress, catalog.EvolutionCatalog);

            _buyButton.onClick.AddListener(OnBuyClicked);
            _progress.OnGoldChanged += RefreshGold;
            _progress.OnArmyChanged += Rebuild;
            _progress.OnOwnedChanged += Rebuild;

            if (_catalog.BaseUnit != null)
                _buyButtonCostLabel.text = _catalog.BaseUnit.Price.ToString();

            Rebuild();
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_progress != null)
                Rebuild();
        }

        private void OnDestroy()
        {
            _buyButton.onClick.RemoveListener(OnBuyClicked);
            _previewFactory?.Dispose();

            if (_progress != null)
            {
                _progress.OnGoldChanged -= RefreshGold;
                _progress.OnArmyChanged -= Rebuild;
                _progress.OnOwnedChanged -= Rebuild;
            }
        }

        private void OnBuyClicked()
        {
            _progress.BuyBaseUnit();
        }

        private void Rebuild()
        {
            ClearContainer(_armyContainer);
            ClearContainer(_reserveContainer);
            _activeCards.Clear();

            var armyStacks = GroupUnits(_progress.ArmyUnits);
            var backlogStacks = GroupUnits(_progress.BacklogUnits);

            foreach (var (unit, count) in armyStacks)
            {
                var card = Instantiate(_cardPrefab, _armyContainer);
                var capturedUnit = unit;
                var portrait = GetOrCreatePortrait(unit);
                card.Init(unit.Name, count, portrait, () => SelectUnit(capturedUnit));
                _activeCards.Add((card, unit));
            }

            foreach (var (unit, count) in backlogStacks)
            {
                var card = Instantiate(_cardPrefab, _reserveContainer);
                var capturedUnit = unit;
                var portrait = GetOrCreatePortrait(unit);
                card.Init(unit.Name, count, portrait, () => SelectUnit(capturedUnit));
                _activeCards.Add((card, unit));
            }

            _slotCountLabel.text = $"{_progress.ArmyUnits.Count}/{_progress.ArmySlots}";
            RefreshGold();
            RefreshEvolutionPanel();
        }

        private void SelectUnit(CharacterData unit)
        {
            _selectedUnit = unit;

            foreach (var (card, data) in _activeCards)
                card.SetSelected(data.Id == unit.Id);

            RefreshEvolutionPanel();
        }

        private void RefreshEvolutionPanel()
        {
            if (_selectedUnit == null)
            {
                _evolutionPanel.Hide();
                return;
            }

            var instances = _progress.GetAllUnitInstances();
            var instance = instances.Find(u => u.Data.Id == _selectedUnit.Id);

            if (instance.Data != null)
                _evolutionPanel.Show(instance.OwnedIndex, instance.Data);
            else
                _evolutionPanel.Hide();
        }

        private RenderTexture GetOrCreatePortrait(CharacterData data)
        {
            if (_portraitCache.TryGetValue(data.Id, out var existing))
                return existing;

            var rt = _previewFactory.CreatePreview(data, _portraitCache.Count);
            _portraitCache[data.Id] = rt;
            return rt;
        }

        private void RefreshGold()
        {
            if (_goldLabel != null)
                _goldLabel.text = _progress.Gold.ToString();

            _buyButton.interactable = _catalog.BaseUnit != null && _progress.CanAfford(_catalog.BaseUnit.Price);
        }

        private static List<(CharacterData unit, int count)> GroupUnits(List<CharacterData> units)
        {
            var grouped = new List<(CharacterData unit, int count)>();
            var counts = new Dictionary<int, int>();
            var first = new Dictionary<int, CharacterData>();

            foreach (var unit in units)
            {
                if (counts.ContainsKey(unit.Id))
                {
                    counts[unit.Id]++;
                }
                else
                {
                    counts[unit.Id] = 1;
                    first[unit.Id] = unit;
                }
            }

            foreach (var kvp in first)
                grouped.Add((kvp.Value, counts[kvp.Key]));

            return grouped;
        }

        private static void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}
