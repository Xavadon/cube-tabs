using System;
using System.Collections.Generic;

namespace _Project.Scripts.Architecture.Services.Save
{
    [Serializable]
    public class SaveData
    {
        public int Gold;
        public int ArmySlots;
        public List<int> OwnedUnitIds = new();
        public List<int> ArmyUnitIds = new();
    }
}
