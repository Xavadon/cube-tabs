using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Inventory
{
    public interface IEquipmentService : IService
    {
        int SlotCount { get; }
        int BackpackCapacity { get; }
        int CraftCapacity { get; }
        IReadOnlyList<ItemData> Slots { get; }
        IReadOnlyList<ItemData> Backpack { get; }
        IReadOnlyList<ItemData> CraftZone { get; }
        bool HasFreeCell { get; }
        StatBonus TotalBonus { get; }

        bool AddToBackpack(ItemData item);
        bool AddToBackpackAt(ItemData item, int cellIndex);
        bool RemoveFromBackpack(ItemData item);
        bool MoveInBackpack(int fromCell, int toCell);
        bool EquipFromBackpack(int cellIndex, int slotIndex);
        bool Equip(ItemData item, int slotIndex);
        bool Unequip(int slotIndex);
        bool UnequipTo(int slotIndex, int cellIndex);
        bool SwapSlots(int fromIndex, int toIndex);
        bool MoveInCraft(int fromIndex, int toIndex);
        bool BackpackToCraft(int cellIndex, int craftIndex);
        bool CraftToBackpack(int craftIndex, int cellIndex);
        bool CraftToEquipment(int craftIndex, int slotIndex);
        bool EquipmentToCraft(int slotIndex, int craftIndex);
        bool SetCraftCell(int craftIndex, ItemData item);

        event Action OnChanged;
    }

    public class EquipmentService : IEquipmentService
    {
        private const int SlotCapacity = 6;
        private const int BackpackCells = 12;
        private const int CraftCells = 6;

        private readonly ItemData[] _slots = new ItemData[SlotCapacity];
        private readonly ItemData[] _backpack = new ItemData[BackpackCells];
        private readonly ItemData[] _craft = new ItemData[CraftCells];

        public int SlotCount => SlotCapacity;
        public int BackpackCapacity => BackpackCells;
        public int CraftCapacity => CraftCells;
        public IReadOnlyList<ItemData> Slots => _slots;
        public IReadOnlyList<ItemData> Backpack => _backpack;
        public IReadOnlyList<ItemData> CraftZone => _craft;
        public bool HasFreeCell => FirstFreeCell() >= 0;
        public StatBonus TotalBonus { get; private set; } = StatBonus.Empty;

        public event Action OnChanged;

        public UniTask Initialize()
        {
            Debug.Log("[EquipmentService] Initialized");
            return UniTask.CompletedTask;
        }

        public bool AddToBackpack(ItemData item)
        {
            return AddToBackpackAt(item, FirstFreeCell());
        }

        public bool AddToBackpackAt(ItemData item, int cellIndex)
        {
            if (item == null || !IsCell(cellIndex) || _backpack[cellIndex] != null)
                return false;

            _backpack[cellIndex] = item;
            OnChanged?.Invoke();
            return true;
        }

        public bool RemoveFromBackpack(ItemData item)
        {
            int index = IndexOf(item);
            if (index < 0)
                return false;

            _backpack[index] = null;
            OnChanged?.Invoke();
            return true;
        }

        public bool MoveInBackpack(int fromCell, int toCell)
        {
            if (fromCell == toCell || !IsCell(fromCell) || !IsCell(toCell) || _backpack[fromCell] == null)
                return false;

            (_backpack[fromCell], _backpack[toCell]) = (_backpack[toCell], _backpack[fromCell]);
            OnChanged?.Invoke();
            return true;
        }

        public bool EquipFromBackpack(int cellIndex, int slotIndex)
        {
            if (!IsCell(cellIndex) || slotIndex < 0 || slotIndex >= SlotCapacity)
                return false;

            ItemData item = _backpack[cellIndex];
            if (item == null)
                return false;

            _backpack[cellIndex] = _slots[slotIndex];
            _slots[slotIndex] = item;

            Recalculate();
            return true;
        }

        public bool Equip(ItemData item, int slotIndex)
        {
            if (item == null || slotIndex < 0 || slotIndex >= SlotCapacity)
                return false;

            int sourceCell = IndexOf(item);

            if (sourceCell >= 0)
                return EquipFromBackpack(sourceCell, slotIndex);

            ItemData previous = _slots[slotIndex];
            int freeCell = FirstFreeCell();

            if (previous != null && freeCell < 0)
                return false;

            _slots[slotIndex] = item;

            if (previous != null)
                _backpack[freeCell] = previous;

            Recalculate();
            return true;
        }

        public bool Unequip(int slotIndex)
        {
            return UnequipTo(slotIndex, FirstFreeCell());
        }

        public bool UnequipTo(int slotIndex, int cellIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCapacity || _slots[slotIndex] == null)
                return false;

            if (!IsCell(cellIndex) || _backpack[cellIndex] != null)
                return false;

            _backpack[cellIndex] = _slots[slotIndex];
            _slots[slotIndex] = null;

            Recalculate();
            return true;
        }

        public bool SwapSlots(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
                return false;

            if (fromIndex < 0 || fromIndex >= SlotCapacity || toIndex < 0 || toIndex >= SlotCapacity)
                return false;

            (_slots[fromIndex], _slots[toIndex]) = (_slots[toIndex], _slots[fromIndex]);

            Recalculate();
            return true;
        }

        // зона крафта — нейтральное хранилище: статов не даёт, поэтому пересчёт бонуса тут не нужен
        public bool MoveInCraft(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex || !IsCraftCell(fromIndex) || !IsCraftCell(toIndex) || _craft[fromIndex] == null)
                return false;

            (_craft[fromIndex], _craft[toIndex]) = (_craft[toIndex], _craft[fromIndex]);
            OnChanged?.Invoke();
            return true;
        }

        public bool BackpackToCraft(int cellIndex, int craftIndex)
        {
            if (!IsCell(cellIndex) || !IsCraftCell(craftIndex) || _backpack[cellIndex] == null)
                return false;

            (_backpack[cellIndex], _craft[craftIndex]) = (_craft[craftIndex], _backpack[cellIndex]);
            OnChanged?.Invoke();
            return true;
        }

        public bool CraftToBackpack(int craftIndex, int cellIndex)
        {
            if (!IsCraftCell(craftIndex) || !IsCell(cellIndex) || _craft[craftIndex] == null)
                return false;

            (_craft[craftIndex], _backpack[cellIndex]) = (_backpack[cellIndex], _craft[craftIndex]);
            OnChanged?.Invoke();
            return true;
        }

        // надетое меняется местами с ячейкой крафта напрямую: снимать через рюкзак ради апгрейда неудобно
        public bool CraftToEquipment(int craftIndex, int slotIndex)
        {
            if (!IsCraftCell(craftIndex) || !IsSlot(slotIndex) || _craft[craftIndex] == null)
                return false;

            (_craft[craftIndex], _slots[slotIndex]) = (_slots[slotIndex], _craft[craftIndex]);

            Recalculate();
            return true;
        }

        public bool EquipmentToCraft(int slotIndex, int craftIndex)
        {
            if (!IsSlot(slotIndex) || !IsCraftCell(craftIndex) || _slots[slotIndex] == null)
                return false;

            (_slots[slotIndex], _craft[craftIndex]) = (_craft[craftIndex], _slots[slotIndex]);

            Recalculate();
            return true;
        }

        public bool SetCraftCell(int craftIndex, ItemData item)
        {
            if (!IsCraftCell(craftIndex))
                return false;

            _craft[craftIndex] = item;
            OnChanged?.Invoke();
            return true;
        }

        private bool IsCell(int index)
        {
            return index >= 0 && index < BackpackCells;
        }

        private bool IsCraftCell(int index)
        {
            return index >= 0 && index < CraftCells;
        }

        private bool IsSlot(int index)
        {
            return index >= 0 && index < SlotCapacity;
        }

        private int IndexOf(ItemData item)
        {
            if (item == null)
                return -1;

            for (int i = 0; i < BackpackCells; i++)
            {
                if (_backpack[i] == item)
                    return i;
            }

            return -1;
        }

        private int FirstFreeCell()
        {
            for (int i = 0; i < BackpackCells; i++)
            {
                if (_backpack[i] == null)
                    return i;
            }

            return -1;
        }

        private void Recalculate()
        {
            StatBonus total = StatBonus.Empty;

            foreach (ItemData item in _slots)
            {
                if (item != null)
                    total += item.Bonus;
            }

            TotalBonus = total;
            OnChanged?.Invoke();
        }
    }
}
