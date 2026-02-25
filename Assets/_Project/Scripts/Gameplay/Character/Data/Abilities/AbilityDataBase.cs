using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    public abstract class AbilityDataBase : ScriptableObject
    {
        [field: SerializeField]
        public float Damage { get; private set; } = 20f;

        [field: SerializeField]
        public DamageType DamageType { get; private set; } = DamageType.Magic;

        public abstract void Execute(Blackboard blackboard);
    }
}
