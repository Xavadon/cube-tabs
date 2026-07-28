using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    [Serializable]
    public class AbilitySlotData
    {
        [field: SerializeField]
        public AbilityDataBase Ability { get; private set; }

        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public Sprite Icon { get; private set; }

        [field: SerializeField]
        public float ManaCost { get; private set; } = 50f;

        [field: SerializeField]
        public float Cooldown { get; private set; } = 8f;
    }
}
