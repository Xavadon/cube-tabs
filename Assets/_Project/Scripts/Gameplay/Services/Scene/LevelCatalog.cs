using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    [CreateAssetMenu(menuName = "Config/LevelCatalog")]
    public class LevelCatalog : ScriptableObject
    {
        public LevelConfig[] Levels;
    }
}
