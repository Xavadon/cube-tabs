using System;

namespace _Project.Scripts.Architecture.BehaviorTree.Leaves
{
    public class Action : BTNode
    {
        private readonly Func<NodeStatus> _action;

        public Action(Func<NodeStatus> action)
        {
            _action = action;
        }

        protected override NodeStatus Process()
        {
            return Status = _action();
        }
    }
}