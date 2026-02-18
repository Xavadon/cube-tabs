using System;
using UnityEngine;

namespace _Project.Scripts.Architecture.BehaviorTree.Decorators
{
    public class Parallel : BTNode
    {
        private readonly BTNode[] _children;

        public Parallel(params BTNode[] children)
        {
            _children = children;
        }

        public override NodeStatus Evaluate()
        {
            foreach (var child in _children)
            {
                child.Evaluate();
            }
            
            return Status = _children[^1].Status;
        }

        public override void Reset()
        {
            base.Reset();
            foreach (var child in _children)
            {
                child.Reset();
            }
        }

        protected override void OnBlackboardSet()
        {
            foreach (var child in _children)
            {
                child.SetBlackboard(Blackboard);
            }
        }
    }
}
