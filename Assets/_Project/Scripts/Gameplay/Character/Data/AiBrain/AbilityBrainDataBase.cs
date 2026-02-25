using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/AbilityBrain")]
    public class AbilityBrainDataBase : RangeBrainDataBase
    {
        [field: SerializeField]
        public AbilityDataBase Ability { get; private set; }

        protected override BTNode CreateAttackNode()
        {
            return new AbilityAttack(Ability, WindUpDuration, AttackDuration, AttackRange);
        }
    }
}
