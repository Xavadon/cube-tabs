using _Project.Scripts.Gameplay.Services.Scene;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/ArmyConfig")]
    public class ArmyConfig : ScriptableObject
    {
        public SpawnEntry[] Units;
    }
}
