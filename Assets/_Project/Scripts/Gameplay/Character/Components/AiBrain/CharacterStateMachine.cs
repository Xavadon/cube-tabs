using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Architecture.Services.Tick;
using _Project.Scripts.Architecture.State_Machine;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Components.AiBrain
{
    public class CharacterStateMachine : StateMachine
    {
        public CharacterStateMachine(AnimatorConroller animatorController, Transform transform, float radius, LayerMask layerMask)
        {
            RegisterState(new IdleState(animatorController));
            RegisterState(new FindTargetState(new TargetFinder(transform, radius, layerMask)));
        }
    }

    public class IdleState : IState
    {
        private readonly AnimatorConroller _animatorController;
        
        public IdleState(AnimatorConroller animatorConroller)
        {
            _animatorController = animatorConroller;
        }
        
        public void Enter()
        {
            _animatorController.PlayIdle();
        }

        public void Exit()
        {
            
        }
    }

    public class FindTargetState : IState, ITickable
    {
        private readonly TargetFinder _targetFinder;

        public FindTargetState(TargetFinder targetFinder)
        {
            _targetFinder = targetFinder;
        }

        public void Exit()
        {
            throw new System.NotImplementedException();
        }

        public void Enter()
        {
            throw new System.NotImplementedException();
        }

        public void Tick(float deltaTime)
        {
            throw new System.NotImplementedException();
        }
    }
    
    public class TargetFinder
    {
        private readonly Transform _transform;
        private readonly float _radius;
        private readonly LayerMask _layer;

        public TargetFinder( Transform transform, float radius, LayerMask layer)
        {
            _transform = transform;
            _radius = radius;
            _layer = layer;
        }

        public Transform TryFindTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(_transform.position, _radius, _layer);

            if (colliders.Length > 0)
            {
                return colliders[0].transform;
            }

            return null;
        }
    }
}