using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.Health;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.Abilities
{
    public abstract class AbilityDataBase : ScriptableObject
    {
        public abstract void Execute(Blackboard blackboard, float damage, DamageType affectedLayers);
    }
}
