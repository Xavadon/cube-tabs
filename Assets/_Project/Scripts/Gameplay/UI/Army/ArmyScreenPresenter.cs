using System;
using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using _Project.Scripts.Gameplay.UI.Shop;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class ArmyScreenPresenter : IDisposable
    {
        private readonly IArmyScreenView _view;
        private readonly IPlayerProgressService _progress;
        private readonly ShopCatalog _catalog;
        private readonly UnitPreviewFactory _portraitFactory;
        private readonly UnitPreviewFactory _fullBodyFactory;
        private readonly Dictionary<int, RenderTexture> _portraitCache = new();
        private readonly Dictionary<int, RenderTexture> _fullBodyCache = new();
        private readonly List<CardEntry> _cardEntries = new();
        private readonly List<(CharacterData unit, int count)> _groupBuffer = new();
        private readonly Dictionary<int, int> _groupCounts = new();
        private readonly Dictionary<int, CharacterData> _groupFirst = new();

        private CardSelection _selection;
        private bool _dirty;

        public ArmyScreenPresenter(
            IArmyScreenView view,
            IPlayerProgressService progress,
            ShopCatalog catalog,
            UnitPreviewConfig portraitConfig,
            UnitPreviewConfig fullBodyConfig)
        {
            _view = view;
            _progress = progress;
            _catalog = catalog;
            _portraitFactory = new UnitPreviewFactory(portraitConfig);
            _fullBodyFactory = new UnitPreviewFactory(fullBodyConfig);

            _view.EvolutionPanel.Initialize(progress, catalog.EvolutionCatalog, _portraitFactory, _portraitCache);

            _view.CardClicked += OnCardClicked;
            _view.BuyClicked += OnBuyClicked;
            _view.TransferClicked += OnTransferClicked;
            _view.ViewEnabled += OnViewEnabled;

            _progress.OnGoldChanged += RefreshGold;
            _progress.OnArmyChanged += ScheduleRebuild;
            _progress.OnOwnedChanged += ScheduleRebuild;

            if (_catalog.BaseUnit != null)
                _view.SetBuyCost(_catalog.BaseUnit.Price.ToString());

            Rebuild();
            _view.SetActive(false);
        }

        public void OnLateUpdate()
        {
            if (!_dirty)
                return;

            _dirty = false;
            Rebuild();
        }

        public void Dispose()
        {
            _portraitFactory?.Dispose();
            _fullBodyFactory?.Dispose();

            _view.BuyClicked -= OnBuyClicked;
            _view.TransferClicked -= OnTransferClicked;
            _view.CardClicked -= OnCardClicked;
            _view.ViewEnabled -= OnViewEnabled;

            _progress.OnGoldChanged -= RefreshGold;
            _progress.OnArmyChanged -= ScheduleRebuild;
            _progress.OnOwnedChanged -= ScheduleRebuild;
        }

        private void ScheduleRebuild() => _dirty = true;

        private void OnViewEnabled() => Rebuild();

        private void OnBuyClicked()
        {
            var baseUnit = _catalog.BaseUnit;
            bool hadSlots = _progress.ArmyUnits.Count < _progress.ArmySlots;

            if (!_progress.BuyBaseUnit())
                return;

            _selection = new CardSelection { Unit = baseUnit, IsInArmy = hadSlots };
        }

        private void OnTransferClicked()
        {
            if (!_selection.HasValue)
                return;

            if (_selection.IsInArmy)
                _progress.RemoveFromArmy(_selection.Unit);
            else
                _progress.AddToArmy(_selection.Unit);
        }

        private void OnCardClicked(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardEntries.Count)
                return;

            var entry = _cardEntries[cardIndex];
            _selection = new CardSelection { Unit = entry.Data, IsInArmy = entry.IsInArmy };

            for (int i = 0; i < _cardEntries.Count; i++)
                _view.SetCardSelected(i, i == cardIndex);

            RefreshFullBodyPreview();
            RefreshEvolutionPanel();
            RefreshTransferButton();
        }

        private void Rebuild()
        {
            _view.ClearCards();
            _cardEntries.Clear();

            SpawnCards(_progress.ArmyUnits, true);
            SpawnCards(_progress.BacklogUnits, false);

            _view.SetSlotCount($"{_progress.ArmyUnits.Count}/{_progress.ArmySlots}");
            RefreshGold();
            RestoreSelection();
            RefreshFullBodyPreview();
            RefreshEvolutionPanel();
            RefreshTransferButton();
        }

        private void SpawnCards(List<CharacterData> units, bool isInArmy)
        {
            GroupUnits(units);

            foreach (var (unit, count) in _groupBuffer)
            {
                var portrait = GetOrCreatePreview(_portraitFactory, _portraitCache, unit);
                _view.AddCard(unit.Name, count, portrait, isInArmy);
                _cardEntries.Add(new CardEntry { Data = unit, IsInArmy = isInArmy });
            }
        }

        private void RestoreSelection()
        {
            if (!_selection.HasValue)
                return;

            bool found = false;

            for (int i = 0; i < _cardEntries.Count; i++)
            {
                var entry = _cardEntries[i];
                bool match = entry.Data.Id == _selection.Unit.Id && entry.IsInArmy == _selection.IsInArmy;
                _view.SetCardSelected(i, match);

                if (match)
                    found = true;
            }

            if (!found)
                _selection.Clear();
        }

        private void RefreshEvolutionPanel()
        {
            if (!_selection.HasValue)
            {
                _view.EvolutionPanel.Hide();
                return;
            }

            var instances = _progress.GetAllUnitInstances();
            var instance = instances.Find(u => u.Data.Id == _selection.Unit.Id);

            if (instance.Data != null)
                _view.EvolutionPanel.Show(instance.OwnedIndex, instance.Data);
            else
                _view.EvolutionPanel.Hide();
        }

        private void RefreshFullBodyPreview()
        {
            if (!_selection.HasValue)
            {
                _view.HideFullBodyPreview();
                return;
            }

            var rt = GetOrCreatePreview(_fullBodyFactory, _fullBodyCache, _selection.Unit);
            _view.ShowFullBodyPreview(rt);
        }

        private void RefreshTransferButton()
        {
            if (!_selection.HasValue)
            {
                _view.SetTransferVisible(false);
                return;
            }

            _view.SetTransferVisible(true);

            if (_selection.IsInArmy)
            {
                _view.SetTransferLabel("В резерв");
                _view.SetTransferInteractable(true);
            }
            else
            {
                _view.SetTransferLabel("В армию");
                _view.SetTransferInteractable(_progress.ArmyUnits.Count < _progress.ArmySlots);
            }
        }

        private void RefreshGold()
        {
            _view.SetGoldText(_progress.Gold.ToString());
            _view.SetBuyInteractable(_catalog.BaseUnit != null && _progress.CanAfford(_catalog.BaseUnit.Price));
        }

        private void GroupUnits(List<CharacterData> units)
        {
            _groupBuffer.Clear();
            _groupCounts.Clear();
            _groupFirst.Clear();

            foreach (var unit in units)
            {
                if (_groupCounts.ContainsKey(unit.Id))
                {
                    _groupCounts[unit.Id]++;
                }
                else
                {
                    _groupCounts[unit.Id] = 1;
                    _groupFirst[unit.Id] = unit;
                }
            }

            foreach (var kvp in _groupFirst)
                _groupBuffer.Add((kvp.Value, _groupCounts[kvp.Key]));
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

        private struct CardSelection
        {
            public CharacterData Unit;
            public bool IsInArmy;

            public bool HasValue => Unit != null;

            public void Clear()
            {
                Unit = null;
                IsInArmy = false;
            }
        }

        private struct CardEntry
        {
            public CharacterData Data;
            public bool IsInArmy;
        }
    }
}
