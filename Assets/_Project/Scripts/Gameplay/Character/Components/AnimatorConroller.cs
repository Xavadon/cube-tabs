using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components
{
    public class AnimatorConroller
    {
        private int _idleHash = Animator.StringToHash("Idle");
        private int _moveHash = Animator.StringToHash("Move");
        private int _attackHash = Animator.StringToHash("Attack");
        private int _rangeAttackHash = Animator.StringToHash("RangeAttack");
        private int _hitHash = Animator.StringToHash("HitReaction");
        
        private readonly Animator _animator;

        public AnimatorConroller(Animator animator)
        {
            _animator = animator;
        }

        private void CrossFade(int hash, float fadeTime = 0.1f)
        {
            _animator.CrossFade(hash, fadeTime);
        }
        
        public void PlayIdle()
        {
            CrossFade(_idleHash);
        }
        
        public void PlayMove()
        {
            CrossFade(_moveHash);
        }
        
        public void PlayAttack()
        {
            CrossFade(_attackHash);
        }
        
        public void PlayRangeAttack()
        {
            CrossFade(_rangeAttackHash);
        }
        
        public void PlayHitReact()
        {
            CrossFade(_hitHash);
        }
    }
}
