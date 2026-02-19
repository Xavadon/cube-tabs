using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components
{
    public class AnimatorConroller
    {
        private int _idleHash = Animator.StringToHash("Idle");
        private int _moveHash = Animator.StringToHash("Move");
        private int _attackHash = Animator.StringToHash("Attack");
        
        private readonly Animator _animator;

        public AnimatorConroller(Animator animator)
        {
            _animator = animator;
        }

        private void CrossFade(int hash, float fadeTime = 0f)
        {
            _animator.CrossFade(hash, fadeTime);
        }
        
        public void PlayIdleAnimation()
        {
            CrossFade(_idleHash);
        }
        
        public void PlayMoveAnimation()
        {
            CrossFade(_moveHash);
        }
        
        public void PlayAttackAnimation()
        {
            CrossFade(_attackHash);
        }
    }
}
