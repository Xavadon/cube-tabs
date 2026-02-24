using System.Collections.Generic;

namespace _Project.Scripts.Architecture.BehaviorTree.Composites
{
    public class Sequence : BTNode
    {
        private readonly List<BTNode> _children = new();

        public Sequence(params BTNode[] children)
        {
            _children.AddRange(children);
        }

        protected override NodeStatus Process()
        {
            for (int i = 0; i < _children.Count; i++)
            {
                Status = _children[i].Evaluate();

                if (Status != NodeStatus.Success)
                    return Status;
            }

            return Status = NodeStatus.Success;
        }

        public override void Reset()
        {
            base.Reset();
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