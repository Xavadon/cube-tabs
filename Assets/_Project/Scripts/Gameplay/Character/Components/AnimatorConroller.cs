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
        private int _hitHash1 = Animator.StringToHash("HitReaction1");

        private int _baseLayerIndex;
        private int _hitLayerIndex;
        
        private readonly Animator _animator;

        public AnimatorConroller(Animator animator)
        {
            _animator = animator;
            _baseLayerIndex = _animator.GetLayerIndex("Base Layer");
            _hitLayerIndex = _animator.GetLayerIndex("Hit Layer");
        }

        private void CrossFade(int hash, float fadeTime = 0.1f, int layerIndex = -1)
        {
            _animator.CrossFade(hash, fadeTime, layerIndex);
        }
        
        public bool IsInState(int hash, int layer = 0)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(layer);

            if (stateInfo.shortNameHash == hash)
            {
                return true;
            }

            return false;
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
            if (!IsInState(_hitHash, _hitLayerIndex))
            {
                CrossFade(_hitHash, 0.15f, _hitLayerIndex);
            }
            else
            {
                CrossFade(_hitHash1, 0.15f, _hitLayerIndex);
            }
        }
    }
}
