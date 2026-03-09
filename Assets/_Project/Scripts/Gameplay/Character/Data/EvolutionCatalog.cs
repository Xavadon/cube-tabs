using System;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data
{
    [CreateAssetMenu(menuName = "Config/EvolutionCatalog")]
    public class EvolutionCatalog : ScriptableObject
    {
        [field: SerializeField]
        public EvolutionEntry[] Entries { get; private set; }

        public EvolutionOption[] GetEvolutions(int sourceId)
        {
            foreach (var entry in Entries)
            {
                if (entry.Source.Id == sourceId)
                    return entry.Options;
            }

            return Array.Empty<EvolutionOption>();
        }
    }

    [Serializable]
    public class EvolutionEntry
    {
        [field: SerializeField]
        public CharacterData Source { get; private set; }

        [field: SerializeField]
        public EvolutionOption[] Options { get; private set; }
    }

    [Serializable]
    public class EvolutionOption
    {
        [field: SerializeField]
        public CharacterData Target { get; private set; }

        [field: SerializeField]
        public int Cost { get; private set; }
    }
}
