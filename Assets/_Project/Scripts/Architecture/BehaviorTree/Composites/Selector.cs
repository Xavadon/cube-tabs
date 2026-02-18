using System.Collections.Generic;

namespace _Project.Scripts.Architecture.BehaviorTree.Composites
{
    public class Selector : BTNode
    {
        private readonly List<BTNode> _children = new();
        private int _currentIndex;
        private int _runningIndex = -1;

        public Selector(params BTNode[] children)
        {
            _children.AddRange(children);
        }

        public override NodeStatus Evaluate()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                Status = _children[i].Evaluate();

                if (Status != NodeStatus.Failure)
                {
                    if (_runningIndex >= 0 && _runningIndex != i)
                        _children[_runningIndex].Reset();

                    _runningIndex = Status == NodeStatus.Running ? i : -1;
                    return Status;
                }
            }

            _runningIndex = -1;
            return Status = NodeStatus.Failure;
        }

        public override void Reset()
        {
            base.Reset();
            _currentIndex = 0;
            _runningIndex = -1;
            _children.ForEach(c => c.Reset());
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