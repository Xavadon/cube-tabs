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
        public int OwnedIndex;
        public CharacterData Data;
        public bool IsInArmy;
    }

    public interface IPlayerProgressService : IService
    {
        int Gold { get; }
        int ArmySlots { get; }
        List<CharacterData> OwnedUnits { get; }
        List<CharacterData> ArmyUnits { get; }
        List<CharacterData> BacklogUnits { get; }

        bool CanAfford(int cost);
        void AddGold(int amount);
        void SpendGold(int amount);
        bool BuyUnit(CharacterData unit);
        bool BuyBaseUnit();
        bool EvolveUnit(int ownedIndex, CharacterData target, int cost);
        List<UnitInstance> GetAllUnitInstances();
        bool TryGetUnitInstance(int unitId, out UnitInstance instance);
        bool UpgradeArmySlots();
        bool AddToArmy(CharacterData unit);
        void RemoveFromArmy(CharacterData unit);
        int GetOwnedCount(CharacterData unit);
        int GetBacklogCount(CharacterData unit);
        int GetSlotUpgradeCost();
        void AddLevelKills(int levelIndex, int kills);
        int GetLevelKills(int levelIndex);
        bool IsLevelCompleted(int levelIndex);
        void MarkLevelCompleted(int levelIndex);
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

            Debug.Log($"[PlayerProgressService] Initialized. Gold: {Gold}, Owned: {_saveData.OwnedUnitIds.Count}, Army: {_saveData.ArmyUnitIds.Count}, Slots: {ArmySlots}");
            return UniTask.CompletedTask;
        }

        public List<CharacterData> OwnedUnits => ResolveUnits(_saveData.OwnedUnitIds);
        public List<CharacterData> ArmyUnits => ResolveUnits(_saveData.ArmyUnitIds);

        public List<CharacterData> BacklogUnits
        {
            get
            {
                // Мультисет-разница: owned минус army (по количеству копий каждого Id)
                var armyCounts = CountById(_saveData.ArmyUnitIds);
                var backlog = new List<CharacterData>();

                foreach (int id in _saveData.OwnedUnitIds)
                {
                    if (armyCounts.TryGetValue(id, out int remaining) && remaining > 0)
                    {
                        armyCounts[id] = remaining - 1;
                        continue;
                    }

                    var unit = _catalog.GetUnitById(id);
                    if (unit != null)
                        backlog.Add(unit);
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

        public bool BuyUnit(CharacterData unit)
        {
            if (!CanAfford(unit.Price))
                return false;

            SpendGold(unit.Price);
            _saveData.OwnedUnitIds.Add(unit.Id);
            OnOwnedChanged?.Invoke();
            Save();
            return true;
        }

        public bool BuyBaseUnit()
        {
            var baseUnit = _catalog.BaseUnit;

            if (baseUnit == null || !CanAfford(baseUnit.Price))
                return false;

            SpendGold(baseUnit.Price);
            _saveData.OwnedUnitIds.Add(baseUnit.Id);

            if (_saveData.ArmyUnitIds.Count < _saveData.ArmySlots)
                _saveData.ArmyUnitIds.Add(baseUnit.Id);

            OnOwnedChanged?.Invoke();
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public bool EvolveUnit(int ownedIndex, CharacterData target, int cost)
        {
            if (ownedIndex < 0 || ownedIndex >= _saveData.OwnedUnitIds.Count)
                return false;

            if (!CanAfford(cost))
                return false;

            int oldId = _saveData.OwnedUnitIds[ownedIndex];
            _saveData.OwnedUnitIds[ownedIndex] = target.Id;

            int armyIdx = _saveData.ArmyUnitIds.IndexOf(oldId);
            if (armyIdx >= 0)
                _saveData.ArmyUnitIds[armyIdx] = target.Id;

            SpendGold(cost);
            OnOwnedChanged?.Invoke();
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public List<UnitInstance> GetAllUnitInstances()
        {
            var armyCounts = CountById(_saveData.ArmyUnitIds);
            var result = new List<UnitInstance>(_saveData.OwnedUnitIds.Count);

            for (int i = 0; i < _saveData.OwnedUnitIds.Count; i++)
            {
                int id = _saveData.OwnedUnitIds[i];
                var data = _catalog.GetUnitById(id);

                if (data == null)
                    continue;

                bool isInArmy = armyCounts.TryGetValue(id, out int remaining) && remaining > 0;

                if (isInArmy)
                    armyCounts[id] = remaining - 1;

                result.Add(new UnitInstance
                {
                    OwnedIndex = i,
                    Data = data,
                    IsInArmy = isInArmy
                });
            }

            return result;
        }

        public bool TryGetUnitInstance(int unitId, out UnitInstance instance)
        {
            for (int i = 0; i < _saveData.OwnedUnitIds.Count; i++)
            {
                if (_saveData.OwnedUnitIds[i] != unitId)
                    continue;

                var data = _catalog.GetUnitById(unitId);
                if (data == null)
                    continue;

                instance = new UnitInstance
                {
                    OwnedIndex = i,
                    Data = data,
                    IsInArmy = _saveData.ArmyUnitIds.Contains(unitId)
                };
                return true;
            }

            instance = default;
            return false;
        }

        public bool UpgradeArmySlots()
        {
            if (_saveData.ArmySlots >= _catalog.MaxArmySlots)
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

        public bool AddToArmy(CharacterData unit)
        {
            if (GetBacklogCount(unit) <= 0)
                return false;

            if (_saveData.ArmyUnitIds.Count >= _saveData.ArmySlots)
                return false;

            _saveData.ArmyUnitIds.Add(unit.Id);
            OnArmyChanged?.Invoke();
            Save();
            return true;
        }

        public void RemoveFromArmy(CharacterData unit)
        {
            _saveData.ArmyUnitIds.Remove(unit.Id);
            OnArmyChanged?.Invoke();
            Save();
        }

        public int GetOwnedCount(CharacterData unit) => CountOccurrences(_saveData.OwnedUnitIds, unit.Id);

        public int GetBacklogCount(CharacterData unit)
        {
            int owned = CountOccurrences(_saveData.OwnedUnitIds, unit.Id);
            int inArmy = CountOccurrences(_saveData.ArmyUnitIds, unit.Id);
            return owned - inArmy;
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

            if (_catalog.AvailableUnits.Length > 0)
            {
                var starterUnit = _catalog.AvailableUnits[0];
                data.OwnedUnitIds.Add(starterUnit.Id);
                data.ArmyUnitIds.Add(starterUnit.Id);
            }

            return data;
        }

        private List<CharacterData> ResolveUnits(List<int> ids)
        {
            var result = new List<CharacterData>(ids.Count);
            foreach (int id in ids)
            {
                var unit = _catalog.GetUnitById(id);
                if (unit != null)
                    result.Add(unit);
                else
                    Debug.LogWarning($"[PlayerProgressService] Unit with Id={id} not found in catalog");
            }

            return result;
        }

        private static int CountOccurrences(List<int> list, int value)
        {
            int count = 0;
            foreach (int item in list)
            {
                if (item == value)
                    count++;
            }

            return count;
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

        private static Dictionary<int, int> CountById(List<int> ids)
        {
            var counts = new Dictionary<int, int>();
            foreach (int id in ids)
            {
                if (counts.ContainsKey(id))
                    counts[id]++;
                else
                    counts[id] = 1;
            }

            return counts;
        }
    }
}
