using System.Collections.Generic;

namespace _Project.Scripts.Architecture.BehaviorTree.Composites
{
    public class Sequence : BTNode
    {
        private readonly List<BTNode> _children = new();
        private int _runningIndex = -1;

        public Sequence(params BTNode[] children)
        {
            _children.AddRange(children);
        }

        protected override NodeStatus Process()
        {
            int startIndex;

            if (_runningIndex >= 0)
            {
                startIndex = _runningIndex;
            }
            else
            {
                startIndex = 0;
            }

            for (int i = startIndex; i < _children.Count; i++)
            {
                NodeStatus childStatus = _children[i].Evaluate();

                if (childStatus == NodeStatus.Failure)
                {
                    _runningIndex = -1;
                    return Status = NodeStatus.Failure;
                }

                if (childStatus == NodeStatus.Running)
                {
                    _runningIndex = i;
                    return Status = NodeStatus.Running;
                }
            }

            _runningIndex = -1;
            return Status = NodeStatus.Success;
        }

        public override void Reset()
        {
            base.Reset();
            _runningIndex = -1;

            foreach (BTNode child in _children)
            {
                child.Reset();
            }
        }

        protected override void OnBlackboardSet()
        {
            foreach (BTNode child in _children)
            {
                child.SetBlackboard(Blackboard);
            }
        }
    }
}