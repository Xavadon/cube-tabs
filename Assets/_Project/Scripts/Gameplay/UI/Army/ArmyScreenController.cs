using System;
using System.Collections.Generic;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Services;
using UnityEngine;

namespace _Project.Scripts.Gameplay.UI.Army
{
    public class ArmyScreenController : IDisposable
    {
        private readonly IArmyScreenView _view;
        private readonly IPlayerProgressService _progress;
        private readonly IUnitPreviewService _previewService;
        private readonly ShopCatalog _catalog;
        private readonly List<CardEntry> _cardEntries = new();
        private readonly List<(ResolvedUnit unit, int count)> _groupBuffer = new();
        private readonly Dictionary<string, int> _groupCounts = new();
        private readonly Dictionary<string, ResolvedUnit> _groupFirst = new();

        private CardSelection _selection;
        private bool _dirty;

        public ArmyScreenController(
            IArmyScreenView view,
            IPlayerProgressService progress,
            IUnitPreviewService previewService,
            ShopCatalog catalog)
        {
            _view = view;
            _progress = progress;
            _previewService = previewService;
            _catalog = catalog;

            _view.CardClicked += OnCardClicked;
            _view.BuyClicked += OnBuyClicked;
            _view.SlotUpgradeClicked += OnSlotUpgradeClicked;
            _view.TransferClicked += OnTransferClicked;
            _view.ViewEnabled += OnViewEnabled;

            _progress.OnArmyChanged += ScheduleRebuild;
            _progress.OnOwnedChanged += ScheduleRebuild;

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
            _view.BuyClicked -= OnBuyClicked;
            _view.SlotUpgradeClicked -= OnSlotUpgradeClicked;
            _view.TransferClicked -= OnTransferClicked;
            _view.CardClicked -= OnCardClicked;
            _view.ViewEnabled -= OnViewEnabled;

            _progress.OnArmyChanged -= ScheduleRebuild;
            _progress.OnOwnedChanged -= ScheduleRebuild;
        }

        private void ScheduleRebuild() => _dirty = true;

        private void OnViewEnabled() => Rebuild();

        private void OnBuyClicked()
        {
            _progress.BuyBaseUnit();
        }

        private void OnSlotUpgradeClicked()
        {
            _progress.UpgradeArmySlots();
        }

        private void OnTransferClicked()
        {
            if (!_selection.HasValue)
                return;

            if (_selection.IsInArmy)
                _progress.RemoveFromArmy(_selection.InstanceId);
            else
                _progress.AddToArmy(_selection.InstanceId);
        }

        private void OnCardClicked(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= _cardEntries.Count)
                return;

            var entry = _cardEntries[cardIndex];
            _selection = new CardSelection
            {
                InstanceId = entry.Resolved.InstanceId,
                Unit = entry.Resolved.Data,
                TierIndex = entry.Resolved.TierIndex,
                IsInArmy = entry.IsInArmy
            };

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

            RestoreSelection();
            RefreshFullBodyPreview();
            RefreshEvolutionPanel();
            RefreshTransferButton();
        }

        private void SpawnCards(List<ResolvedUnit> units, bool isInArmy)
        {
            GroupUnits(units);

            foreach (var (unit, count) in _groupBuffer)
            {
                var portrait = _previewService.GetPortrait(unit.Data, unit.TierIndex);
                string displayName;

                if (unit.Data.MaxTier > 0)
                {
                    displayName = $"{unit.Data.Name} T{unit.TierIndex + 1}";
                }
                else
                {
                    displayName = unit.Data.Name;
                }

                var tier = unit.Data.GetTier(unit.TierIndex);
                float hp = tier.Stats.Health;
                float damage = tier.Stats.Damage;
                float speed = tier.MoveSpeed;

                _view.AddCard(displayName, count, portrait, hp, damage, speed, isInArmy);
                _cardEntries.Add(new CardEntry { Resolved = unit, IsInArmy = isInArmy });
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
                bool match = entry.Resolved.Data.Id == _selection.Unit.Id
                             && entry.Resolved.TierIndex == _selection.TierIndex
                             && entry.IsInArmy == _selection.IsInArmy;
                _view.SetCardSelected(i, match);

                if (match)
                {
                    _selection.InstanceId = entry.Resolved.InstanceId;
                    found = true;
                }
            }

            if (!found)
                _selection.Clear();
        }

        private void RefreshEvolutionPanel()
        {
            if (!_selection.HasValue)
            {
                _view.HideEvolution();
                return;
            }

            if (_progress.TryGetUnitInstance(_selection.InstanceId, out var instance))
                _view.ShowEvolution(instance.InstanceId, instance.Data, instance.TierIndex);
            else
                _view.HideEvolution();
        }

        private void RefreshFullBodyPreview()
        {
            if (!_selection.HasValue)
            {
                _view.HideFullBodyPreview();
                return;
            }

            var handle = _previewService.GetFullBody(_selection.Unit, _selection.TierIndex);
            _view.ShowFullBodyPreview(handle);
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

        private void GroupUnits(List<ResolvedUnit> units)
        {
            _groupBuffer.Clear();
            _groupCounts.Clear();
            _groupFirst.Clear();

            foreach (var unit in units)
            {
                string key = $"{unit.Data.Id}_{unit.TierIndex}";

                if (_groupCounts.ContainsKey(key))
                {
                    _groupCounts[key]++;
                }
                else
                {
                    _groupCounts[key] = 1;
                    _groupFirst[key] = unit;
                }
            }

            foreach (var kvp in _groupFirst)
                _groupBuffer.Add((kvp.Value, _groupCounts[kvp.Key]));
        }

        private struct CardSelection
        {
            public int InstanceId;
            public CharacterData Unit;
            public int TierIndex;
            public bool IsInArmy;

            public bool HasValue => Unit != null;

            public void Clear()
            {
                Unit = null;
                IsInArmy = false;
                InstanceId = -1;
                TierIndex = 0;
            }
        }

        private struct CardEntry
        {
            public ResolvedUnit Resolved;
            public bool IsInArmy;
        }
    }
}
