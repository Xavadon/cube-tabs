using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    public abstract class AbilityDataBase : ScriptableObject
    {
        [field: SerializeField]
        public float DamageMultiplier { get; private set; } = 1f;

        public abstract void Execute(Blackboard blackboard, float damage, DamageType affectedLayers);

        protected float ApplyMultiplier(float damage) => damage * DamageMultiplier;
    }
}
