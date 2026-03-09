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

        [Header("Transfer")]
        [SerializeField]
        private Button _transferButton;

        [SerializeField]
        private TextMeshProUGUI _transferButtonLabel;

        [Header("Full Body Preview")]
        [SerializeField]
        private RawImage _fullBodyPreviewImage;

        [Header("Evolution")]
        [SerializeField]
        private EvolutionPanelUI _evolutionPanel;

        private IPlayerProgressService _progress;
        private ShopCatalog _catalog;
        private UnitPreviewFactory _portraitFactory;
        private UnitPreviewFactory _fullBodyFactory;
        private readonly Dictionary<int, RenderTexture> _portraitCache = new();
        private readonly Dictionary<int, RenderTexture> _fullBodyCache = new();
        private ArmyUnitCardUI _selectedCard;
        private CharacterData _selectedUnit;
        private bool _selectedIsInArmy;
        private readonly List<(ArmyUnitCardUI card, CharacterData data, bool isInArmy)> _activeCards = new();

        public void Initialize(IPlayerProgressService progress, ShopCatalog catalog,
            UnitPreviewConfig portraitConfig, UnitPreviewConfig fullBodyConfig)
        {
            _progress = progress;
            _catalog = catalog;
            _portraitFactory = new UnitPreviewFactory(portraitConfig);
            _fullBodyFactory = new UnitPreviewFactory(fullBodyConfig);

            _evolutionPanel.Initialize(progress, catalog.EvolutionCatalog, _portraitFactory, _portraitCache);

            _buyButton.onClick.AddListener(OnBuyClicked);
            _transferButton.onClick.AddListener(OnTransferClicked);
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
            _transferButton.onClick.RemoveListener(OnTransferClicked);
            _portraitFactory?.Dispose();
            _fullBodyFactory?.Dispose();

            if (_progress != null)
            {
                _progress.OnGoldChanged -= RefreshGold;
                _progress.OnArmyChanged -= Rebuild;
                _progress.OnOwnedChanged -= Rebuild;
            }
        }

        private void OnBuyClicked()
        {
            _selectedUnit = _catalog.BaseUnit;
            bool hadSlots = _progress.ArmyUnits.Count < _progress.ArmySlots;
            _selectedIsInArmy = hadSlots;

            if (!_progress.BuyBaseUnit())
            {
                _selectedUnit = null;
            }
        }

        private void OnTransferClicked()
        {
            if (_selectedUnit == null)
                return;

            if (_selectedIsInArmy)
                _progress.RemoveFromArmy(_selectedUnit);
            else
                _progress.AddToArmy(_selectedUnit);
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
                var capturedCard = card;
                var portrait = GetOrCreatePreview(_portraitFactory, _portraitCache, unit);
                card.Init(unit.Name, count, portrait, () => SelectCard(capturedCard, capturedUnit, true));
                _activeCards.Add((card, unit, true));
            }

            foreach (var (unit, count) in backlogStacks)
            {
                var card = Instantiate(_cardPrefab, _reserveContainer);
                var capturedUnit = unit;
                var capturedCard = card;
                var portrait = GetOrCreatePreview(_portraitFactory, _portraitCache, unit);
                card.Init(unit.Name, count, portrait, () => SelectCard(capturedCard, capturedUnit, false));
                _activeCards.Add((card, unit, false));
            }

            _slotCountLabel.text = $"{_progress.ArmyUnits.Count}/{_progress.ArmySlots}";
            RefreshGold();

            if (_selectedUnit != null)
            {
                bool found = false;

                foreach (var (card, data, isInArmy) in _activeCards)
                {
                    bool match = data.Id == _selectedUnit.Id && isInArmy == _selectedIsInArmy;
                    card.SetSelected(match);

                    if (match)
                    {
                        _selectedCard = card;
                        found = true;
                    }
                }

                if (!found)
                {
                    _selectedCard = null;
                    _selectedUnit = null;
                }
            }

            RefreshFullBodyPreview();
            RefreshEvolutionPanel();
            RefreshTransferButton();
        }

        private void SelectCard(ArmyUnitCardUI clickedCard, CharacterData unit, bool isInArmy)
        {
            _selectedCard = clickedCard;
            _selectedUnit = unit;
            _selectedIsInArmy = isInArmy;

            foreach (var (card, _, _) in _activeCards)
                card.SetSelected(card == clickedCard);

            RefreshFullBodyPreview();
            RefreshEvolutionPanel();
            RefreshTransferButton();
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

        private void RefreshFullBodyPreview()
        {
            if (_fullBodyPreviewImage == null)
                return;

            if (_selectedUnit == null)
            {
                _fullBodyPreviewImage.gameObject.SetActive(false);
                return;
            }

            var rt = GetOrCreatePreview(_fullBodyFactory, _fullBodyCache, _selectedUnit);
            _fullBodyPreviewImage.texture = rt;
            _fullBodyPreviewImage.gameObject.SetActive(true);
        }

        private static RenderTexture GetOrCreatePreview(
            UnitPreviewFactory factory, Dictionary<int, RenderTexture> cache, CharacterData data)
        {
            if (cache.TryGetValue(data.Id, out var existing))
                return existing;

            var rt = factory.CreatePreview(data, cache.Count);
            cache[data.Id] = rt;
            return rt;
        }

        private void RefreshTransferButton()
        {
            if (_selectedUnit == null)
            {
                _transferButton.gameObject.SetActive(false);
                return;
            }

            _transferButton.gameObject.SetActive(true);

            if (_selectedIsInArmy)
            {
                _transferButtonLabel.text = "В резерв";
                _transferButton.interactable = true;
            }
            else
            {
                _transferButtonLabel.text = "В армию";
                _transferButton.interactable = _progress.ArmyUnits.Count < _progress.ArmySlots;
            }
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
