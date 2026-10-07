using System.Collections.Generic;

namespace _Project.Scripts.Architecture.BehaviorTree.Composites
{
    public class Selector : BTNode
    {
        private readonly List<BTNode> _children = new();
        private int _runningIndex = -1;

        public Selector(params BTNode[] children)
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
                    continue;
                }

                if (_runningIndex >= 0 && _runningIndex != i)
                {
                    _children[_runningIndex].Reset();
                }

                if (childStatus == NodeStatus.Running)
                {
                    _runningIndex = i;
                }
                else
                {
                    _runningIndex = -1;
                }

                return Status = childStatus;
            }

            _runningIndex = -1;
            return Status = NodeStatus.Failure;
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