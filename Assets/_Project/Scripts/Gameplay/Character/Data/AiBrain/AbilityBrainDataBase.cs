using System;
using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Components;
using _Project.Scripts.Gameplay.Character.Components.AiBrain;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    [CreateAssetMenu(menuName = "Config/Ai/AbilityBrain")]
    public class AbilityBrainDataBase : RangeBrainDataBase
    {
        protected override BTNode CreateAttackNode(TierData tier)
        {
            return new AbilityAttack(tier.Ability, WindUpDuration, AttackDuration, AttackRange,
                ResolveAnimation(tier.AnimationType));
        }

        public static Action<AnimatorConroller> ResolveAnimation(AbilityAnimationType type) => type switch
        {
            AbilityAnimationType.RangeAttack => a => a.PlayRangeAttack(),
            AbilityAnimationType.AbilityAttack => a => a.PlayAbilityAttack(),
            _ => a => a.PlayAbilityAttack(),
        };
    }
}
