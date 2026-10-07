using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [Serializable]
    public class CharacterResistancesData
    {
        [field: SerializeField]
        public float PhysicalResist { get; private set; } = 0.1f;

        [field: SerializeField]
        public float MagicResist { get; private set; } = 0.25f;

        [field: SerializeField]
        public float FireResist { get; private set; } = 0.1f;

        [field: SerializeField]
        public float FaithResist { get; private set; } = 0.15f;
    }
}
