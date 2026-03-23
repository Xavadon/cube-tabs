using System;
using System.Collections.Generic;

namespace _Project.Scripts.Architecture.Services.Save
{
    [Serializable]
    public class LevelKillEntry
    {
        public int LevelIndex;
        public int Kills;
    }

    [Serializable]
    public class OwnedUnit
    {
        public int InstanceId;
        public string UnitId;
        public int TierIndex;
    }

    [Serializable]
    public class SaveData
    {
        public int Gold;
        public int ArmySlots;
        public int NextInstanceId;
        public List<OwnedUnit> OwnedUnits = new();
        public List<int> ArmyInstanceIds = new();
        public List<LevelKillEntry> LevelKillProgress = new();
        public List<int> CompletedLevelIndices = new();
        public List<int> RewardedLevelIndices = new();
        public int BonusMaxArmySlots;
    }
}
