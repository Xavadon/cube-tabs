using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Save;
using _Project.Scripts.Gameplay.Services.Scene;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services
{
    public struct UnitInstance
    {
        public int InstanceId;
        public CharacterData Data;
        public int TierIndex;
        public bool IsInArmy;
    }

    public interface IPlayerProgressService : IService
    {
        int Gold { get; }
        int ArmySlots { get; }
        int MaxArmySlots { get; }
        List<ResolvedUnit> ArmyUnits { get; }
        List<ResolvedUnit> BacklogUnits { get; }

        bool CanAfford(int cost);
        void AddGold(int amount);
        void SpendGold(int amount);
        bool BuyBaseUnit();
        bool BuyUniqueUnit(CharacterData unit);
        bool IsUnitOwned(string unitId);
        void GrantItemReward(ShopItemData item);
        void GrantUnit(CharacterData unit);
        void GrantArmySlots(int count);
        bool EvolveUnit(int instanceId, CharacterData target, int cost);
        bool UpgradeTier(int instanceId);
        bool TryGetUnitInstance(int unitId, out UnitInstance instance);
        bool UpgradeArmySlots();
        bool AddToArmy(int instanceId);
        void RemoveFromArmy(int instanceId);
        int GetSlotUpgradeCost();
        void AddLevelKills(int levelIndex, int kills);
        int GetLevelKills(int levelIndex);
        bool IsLevelCompleted(int levelIndex);
        void MarkLevelCompleted(int levelIndex);
        bool IsMilestoneClaimed(int levelIndex, int milestoneIndex);
        void ClaimMilestone(int levelIndex, int milestoneIndex);
        void Save();

        event Action OnGoldChanged;
        event Action OnArmyChanged;
        event Action OnOwnedChanged;
    }

    public class PlayerProgressService : IPlayerProgressService
    {
        private const string CatalogPath = "Data/ShopCatalog";

        private readonly ISaveService _saveService;

        private ShopCatalog _catalog;
        private SaveData _saveData;

        public int Gold => _saveData.Gold;
        public int ArmySlots => _saveData.ArmySlots;
        public int MaxArmySlots => _catalog.MaxArmySlots + _saveData.BonusMaxArmySlots;

        public event Action OnGoldChanged;
        public event Action OnArmyChanged;
        public event Action OnOwnedChanged;

        public PlayerProgressService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public UniTask Initialize()
        {
            _catalog = Resources.Load<ShopCatalog>(CatalogPath);

            if (_catalog == null)
            {
                Debug.LogError($"[PlayerProgressService] ShopCatalog not found at '{CatalogPath}'");
                return UniTask.CompletedTask;
            }

            if (_saveService.HasSave())
            {
                _saveData = _saveService.Load();
            }
            else
            {
                _saveData = CreateDefaultSave();
                _saveService.Save(_saveData);
            }

            Debug.Log($"[PlayerProgressService] Initialized. Gold: {Gold}, Owned: {_saveData.OwnedUnits.Count}, Army: {_saveData.ArmyInstanceIds.Count}, Slots: {ArmySlots}");
            return UniTask.CompletedTask;
        }

        public List<ResolvedUnit> ArmyUnits
        {
            get
            {
                var result = new List<ResolvedUnit>();
                foreach (int instanceId in _saveData.ArmyInstanceIds)
                {
                    var owned = FindOwnedUnit(instanceId);
                    if (owned == null) continue;

                    var data = _catalog.GetUnitById(owned.UnitId);
                    if (data != null)
                        result.Add(new ResolvedUnit { InstanceId = owned.InstanceId, Data = data, TierIndex = owned.TierIndex });
                }
                return result;
            }
        }

        public List<ResolvedUnit> BacklogUnits
        {
            get
            {
                var backlog = new List<ResolvedUnit>();
                foreach (var owned in _saveData.OwnedUnits)
                {
                    if (_saveData.ArmyInstanceIds.Contains(owned.InstanceId))
                        continue;

                    var data = _catalog.GetUnitById(owned.UnitId);
                    if (data != null)
                        backlog.Add(new ResolvedUnit { InstanceId = owned.InstanceId, Data = data, TierIndex = owned.TierIndex });
                }
                return backlog;
            }
        }

        public bool CanAfford(int cost) => _saveData.Gold >= cost;

        public void AddGold(int amount)
        {
            _saveData.Gold += amount;
            OnGoldChanged?.Invoke();
        }

        public void SpendGold(int amount)
        {
            _saveData.Gold -= amount;
            OnGoldChanged?.Invoke();
        }

        public bool BuyBaseUnit()
        {
            var baseUnit = _catalog.BaseUnit;

            if (baseUnit == null || !CanAfford(baseUnit.PriceAsHero))
                return false;

            SpendGold(baseUnit.PriceAsHero);

            int instanceId = _saveData.NextInstanceId++;
            _saveData.OwnedUnits.Add(new OwnedUnit
            {
                InstanceId = instanceId,
                UnitId = baseUnit.Id,
                TierIndex = 0
            });

            if (_saveData.ArmyInstanceIds.Count < _saveData.ArmySlots)
                _saveData.ArmyInstanceIds.Add(instanceId);

            OnOwnedChanged?.Invoke();
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public bool BuyUniqueUnit(CharacterData unit)
        {
            if (unit == null || !CanAfford(unit.PriceAsHero))
                return false;

            if (IsUnitOwned(unit.Id))
                return false;

            SpendGold(unit.PriceAsHero);

            int instanceId = _saveData.NextInstanceId++;
            _saveData.OwnedUnits.Add(new OwnedUnit
            {
                InstanceId = instanceId,
                UnitId = unit.Id,
                TierIndex = 0
            });

            if (_saveData.ArmyInstanceIds.Count < _saveData.ArmySlots)
                _saveData.ArmyInstanceIds.Add(instanceId);

            OnOwnedChanged?.Invoke();
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public void GrantUnit(CharacterData unit)
        {
            if (unit == null)
                return;

            int instanceId = _saveData.NextInstanceId++;
            _saveData.OwnedUnits.Add(new OwnedUnit
            {
                InstanceId = instanceId,
                UnitId = unit.Id,
                TierIndex = 0
            });

            if (_saveData.ArmyInstanceIds.Count < _saveData.ArmySlots)
                _saveData.ArmyInstanceIds.Add(instanceId);

            OnOwnedChanged?.Invoke();
            OnArmyChanged?.Invoke();
            Save();
        }

        public void GrantArmySlots(int count)
        {
            _saveData.ArmySlots += count;
            _saveData.BonusMaxArmySlots += count;
            OnArmyChanged?.Invoke();
            Save();
        }

        public bool IsUnitOwned(string unitId)
        {
            foreach (var owned in _saveData.OwnedUnits)
            {
                if (owned.UnitId == unitId)
                    return true;
            }

            return false;
        }

        public void GrantItemReward(ShopItemData item)
        {
            switch (item.RewardType)
            {
                case ShopItemRewardType.Gold:
                    AddGold(item.RewardAmount);
                    break;
                case ShopItemRewardType.ArmySlot:
                    _saveData.ArmySlots += item.RewardAmount;
                    OnArmyChanged?.Invoke();
                    Save();
                    break;
            }
        }

        public bool EvolveUnit(int instanceId, CharacterData target, int cost)
        {
            var owned = FindOwnedUnit(instanceId);
            if (owned == null)
                return false;

            if (!CanAfford(cost))
                return false;

            owned.UnitId = target.Id;
            owned.TierIndex = 0;

            SpendGold(cost);
            OnOwnedChanged?.Invoke();
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public bool UpgradeTier(int instanceId)
        {
            var owned = FindOwnedUnit(instanceId);
            if (owned == null)
                return false;

            var data = _catalog.GetUnitById(owned.UnitId);
            if (data == null || owned.TierIndex >= data.MaxTier)
                return false;

            var currentTier = data.GetTier(owned.TierIndex);
            int nextTierCost = data.GetTier(owned.TierIndex + 1).EvolutionCost;

            if (!CanAfford(nextTierCost))
                return false;

            SpendGold(nextTierCost);
            owned.TierIndex++;

            OnOwnedChanged?.Invoke();
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public bool TryGetUnitInstance(int instanceId, out UnitInstance instance)
        {
            var owned = FindOwnedUnit(instanceId);
            if (owned == null)
            {
                instance = default;
                return false;
            }

            var data = _catalog.GetUnitById(owned.UnitId);
            if (data == null)
            {
                instance = default;
                return false;
            }

            instance = new UnitInstance
            {
                InstanceId = owned.InstanceId,
                Data = data,
                TierIndex = owned.TierIndex,
                IsInArmy = _saveData.ArmyInstanceIds.Contains(owned.InstanceId)
            };
            return true;
        }

        public bool UpgradeArmySlots()
        {
            if (_saveData.ArmySlots >= MaxArmySlots)
                return false;

            int cost = GetSlotUpgradeCost();

            if (!CanAfford(cost))
                return false;

            SpendGold(cost);
            _saveData.ArmySlots++;
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public bool AddToArmy(int instanceId)
        {
            var owned = FindOwnedUnit(instanceId);
            if (owned == null)
                return false;

            if (_saveData.ArmyInstanceIds.Contains(instanceId))
                return false;

            if (_saveData.ArmyInstanceIds.Count >= _saveData.ArmySlots)
                return false;

            _saveData.ArmyInstanceIds.Add(instanceId);
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public void RemoveFromArmy(int instanceId)
        {
            _saveData.ArmyInstanceIds.Remove(instanceId);
            OnArmyChanged?.Invoke();
            Save();
        }

        public int GetSlotUpgradeCost() => _catalog.SlotUpgradeCost;

        public void AddLevelKills(int levelIndex, int kills)
        {
            var entry = FindOrCreateKillEntry(levelIndex);
            entry.Kills += kills;
            Save();
        }

        public int GetLevelKills(int levelIndex)
        {
            var entry = _saveData.LevelKillProgress.Find(e => e.LevelIndex == levelIndex);
            return entry?.Kills ?? 0;
        }

        public bool IsLevelCompleted(int levelIndex)
        {
            return _saveData.CompletedLevelIndices.Contains(levelIndex);
        }

        public void MarkLevelCompleted(int levelIndex)
        {
            if (!_saveData.CompletedLevelIndices.Contains(levelIndex))
            {
                _saveData.CompletedLevelIndices.Add(levelIndex);
                Save();
            }
        }

        public bool IsMilestoneClaimed(int levelIndex, int milestoneIndex)
        {
            foreach (var entry in _saveData.ClaimedMilestones)
            {
                if (entry.LevelIndex == levelIndex && entry.MilestoneIndex == milestoneIndex)
                    return true;
            }

            return false;
        }

        public void ClaimMilestone(int levelIndex, int milestoneIndex)
        {
            if (IsMilestoneClaimed(levelIndex, milestoneIndex))
                return;

            _saveData.ClaimedMilestones.Add(new ClaimedMilestoneEntry
            {
                LevelIndex = levelIndex,
                MilestoneIndex = milestoneIndex
            });
            Save();
        }

        public void Save()
        {
            _saveService.Save(_saveData);
        }

        private SaveData CreateDefaultSave()
        {
            var data = new SaveData
            {
                Gold = _catalog.StartingGold,
                ArmySlots = _catalog.BaseArmySlots
            };

            if (_catalog.GrantAllUnitsOnStart && _catalog.AvailableUnits != null)
            {
                foreach (var unit in _catalog.AvailableUnits)
                {
                    int instanceId = data.NextInstanceId++;
                    data.OwnedUnits.Add(new OwnedUnit
                    {
                        InstanceId = instanceId,
                        UnitId = unit.Id,
                        TierIndex = 0
                    });

                    if (data.ArmyInstanceIds.Count < data.ArmySlots)
                        data.ArmyInstanceIds.Add(instanceId);
                }
            }
            else if (_catalog.StartingUnit != null)
            {
                int instanceId = data.NextInstanceId++;
                data.OwnedUnits.Add(new OwnedUnit
                {
                    InstanceId = instanceId,
                    UnitId = _catalog.StartingUnit.Id,
                    TierIndex = 0
                });
                data.ArmyInstanceIds.Add(instanceId);
            }

            return data;
        }

        private OwnedUnit FindOwnedUnit(int instanceId)
        {
            foreach (var owned in _saveData.OwnedUnits)
            {
                if (owned.InstanceId == instanceId)
                    return owned;
            }
            return null;
        }

        private LevelKillEntry FindOrCreateKillEntry(int levelIndex)
        {
            var entry = _saveData.LevelKillProgress.Find(e => e.LevelIndex == levelIndex);
            if (entry == null)
            {
                entry = new LevelKillEntry { LevelIndex = levelIndex, Kills = 0 };
                _saveData.LevelKillProgress.Add(entry);
            }

            return entry;
        }
    }
}
