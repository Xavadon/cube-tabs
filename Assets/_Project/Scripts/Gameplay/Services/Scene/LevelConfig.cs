using System;
using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    [CreateAssetMenu(menuName = "Config/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public SpawnEntry[] Enemies;
        public SpawnEntry[] Allies;
    }

    [Serializable]
    public class SpawnEntry
    {
        public CharacterData CharacterData;
        public int Count = 1;
    }
}
