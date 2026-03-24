using System;
using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    [CreateAssetMenu(menuName = "Config/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [field: SerializeField]
        public string LevelName { get; private set; } = "Level";

        [field: SerializeField]
        public int LevelIndex { get; private set; }

        [field: SerializeField]
        public KillMilestone[] Milestones { get; private set; }

        public WaveData[] Waves;
    }

    [Serializable]
    public class KillMilestone
    {
        [field: SerializeField]
        public int KillsRequired { get; private set; }

        [field: SerializeField]
        public RewardEntry[] Rewards { get; private set; }

        [field: SerializeField]
        public int BonusArmySlots { get; private set; }
    }

    [Serializable]
    public class RewardEntry
    {
        public CharacterData CharacterData;
        public int Count = 1;
    }

    [Serializable]
    public class WaveData
    {
        public SpawnEntry[] Entries;
    }

    [Serializable]
    public class SpawnEntry
    {
        public CharacterData CharacterData;
        public int TierIndex;
        public int Count = 1;
    }
}
