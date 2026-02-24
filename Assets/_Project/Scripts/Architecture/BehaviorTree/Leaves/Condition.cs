using System;

namespace _Project.Scripts.Architecture.BehaviorTree.Leaves
{
    public class Condition : BTNode
    {
        private readonly Func<bool> _predicate;

        public Condition(Func<bool> predicate)
        {
            _predicate = predicate;
        }

        protected override NodeStatus Process()
        {
            if (_predicate())
            {
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Failure;
        }
    }
}