using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [Serializable]
    public class CharacterStatsData
    {
        [field: SerializeField]
        public float Health { get; private set; } = 100f;

        [field: SerializeField]
        public float Mana { get; private set; } = 100f;

        [field: SerializeField]
        public float Stamina { get; private set; } = 100f;
    }
}
