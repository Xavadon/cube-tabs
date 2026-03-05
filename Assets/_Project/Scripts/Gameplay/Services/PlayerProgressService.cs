using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Save;
using _Project.Scripts.Gameplay.Character.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services
{
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
        bool UpgradeArmySlots();
        bool AddToArmy(CharacterData unit);
        void RemoveFromArmy(CharacterData unit);
        int GetOwnedCount(CharacterData unit);
        int GetBacklogCount(CharacterData unit);
        int GetSlotUpgradeCost();
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
