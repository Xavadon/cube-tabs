using System.Collections.Generic;

namespace _Project.Scripts.Architecture.BehaviorTree.Composites
{
    /// <summary>
    /// Selector that always evaluates from index 0, allowing higher-priority
    /// children to interrupt lower-priority Running children.
    /// </summary>
    public class ReactiveSelector : BTNode
    {
        private readonly List<BTNode> _children = new();
        private int _runningIndex = -1;
        private int _lockedIndex = -1;

        public ReactiveSelector(params BTNode[] children)
        {
            _children.AddRange(children);
        }

        protected override NodeStatus Process()
        {
            int startIndex = 0;
            
            if (_lockedIndex >= 0)
            {
                startIndex = _lockedIndex;
            }
            
            for (int i = startIndex; i < _children.Count; i++)
            {
                NodeStatus childStatus = _children[i].Evaluate();

                if (childStatus == NodeStatus.Locked)
                {
                    _lockedIndex = i;
                    return NodeStatus.Running;
                }

                _lockedIndex = 0;
                
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
