using System.Collections.Generic;

namespace _Project.Scripts.Architecture.BehaviorTree.Composites
{
    public class Sequence : BTNode
    {
        private readonly List<BTNode> _children = new();
        private int _currentIndex;

        public Sequence(params BTNode[] children)
        {
            _children.AddRange(children);
        }

        public override NodeStatus Evaluate()
        {
            while (_currentIndex < _children.Count)
            {
                Status = _children[_currentIndex].Evaluate();

                if (Status != NodeStatus.Success)
                    return Status;

                _currentIndex++;
            }

            _currentIndex = 0;
            return Status = NodeStatus.Success;
        }

        public override void Reset()
        {
            base.Reset();
            _currentIndex = 0;
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