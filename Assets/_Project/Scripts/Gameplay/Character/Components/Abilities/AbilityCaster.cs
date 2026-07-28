using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Mana;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.Abilities
{
    public class AbilityCaster
    {
        private readonly IReadOnlyList<AbilitySlotData> _slots;
        private readonly ManaComponent _mana;
        private readonly Blackboard _blackboard;
        private readonly float[] _readyAt;

        private CharacterStats _stats;

        public IReadOnlyList<AbilitySlotData> Slots => _slots;

        public event Action OnChanged;

        public AbilityCaster(IReadOnlyList<AbilitySlotData> slots, ManaComponent mana, Blackboard blackboard,
            CharacterStats stats)
        {
            _slots = slots;
            _mana = mana;
            _blackboard = blackboard;
            _stats = stats;
            _readyAt = new float[slots.Count];
        }

        public void SetStats(CharacterStats stats)
        {
            _stats = stats;
        }

        public float CooldownLeft(int index)
        {
            return IsSlot(index) ? Mathf.Max(0f, _readyAt[index] - Time.time) : 0f;
        }

        public float CooldownRatio(int index)
        {
            if (!IsSlot(index) || _slots[index].Cooldown <= 0f)
                return 0f;

            return Mathf.Clamp01(CooldownLeft(index) / _slots[index].Cooldown);
        }

        public bool CanCast(int index)
        {
            return IsSlot(index)
                   && _slots[index].Ability != null
                   && CooldownLeft(index) <= 0f
                   && _mana.Has(_slots[index].ManaCost);
        }

        public bool TryCast(int index)
        {
            if (!CanCast(index))
                return false;

            AbilitySlotData slot = _slots[index];

            if (!_mana.Spend(slot.ManaCost))
                return false;

            slot.Ability.Execute(_blackboard, _stats.Damage, _stats.DamageType);

            _readyAt[index] = Time.time + slot.Cooldown;
            OnChanged?.Invoke();

            return true;
        }

        private bool IsSlot(int index)
        {
            return index >= 0 && index < _slots.Count;
        }
    }
}
