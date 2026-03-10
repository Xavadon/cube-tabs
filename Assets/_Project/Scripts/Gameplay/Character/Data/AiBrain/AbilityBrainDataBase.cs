using System;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using _Project.Scripts.Gameplay.Character.Data.Abilities;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    public enum AbilityAnimationType
    {
        RangeAttack,
        AbilityAttack,
    }

    [CreateAssetMenu(menuName = "Config/Ai/AbilityBrain")]
    public class AbilityBrainDataBase : RangeBrainDataBase
    {
        [field: SerializeField]
        public AbilityDataBase Ability { get; private set; }

        [field: SerializeField]
        public AbilityAnimationType AnimationType { get; private set; }

        protected override BTNode CreateAttackNode()
        {
            return new AbilityAttack(Ability, WindUpDuration, AttackDuration, AttackRange,
                ResolveAnimation(AnimationType));
        }

        private static Action<AnimatorConroller> ResolveAnimation(AbilityAnimationType type) => type switch
        {
            AbilityAnimationType.RangeAttack => a => a.PlayRangeAttack(),
            AbilityAnimationType.AbilityAttack => a => a.PlayAbilityAttack(),
            _ => a => a.PlayAbilityAttack(),
        };
    }
}
