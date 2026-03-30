using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components
{
    public class AnimatorController
    {
        private readonly int _idleHash = Animator.StringToHash("Idle");
        private readonly int _moveHash = Animator.StringToHash("Move");
        private readonly int _attackHash = Animator.StringToHash("Attack");
        private readonly int _hitHash = Animator.StringToHash("HitReaction");
        private readonly int _hitHash1 = Animator.StringToHash("HitReaction1");

        private readonly int _baseLayerIndex;
        private readonly int _hitLayerIndex;

        private readonly Animator _animator;

        public AnimatorController(Animator animator)
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
            if (IsInState(_idleHash, _baseLayerIndex))
            {
                return;
            }
            
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
