namespace _Project.Scripts.Architecture.BehaviorTree.Decorators
{
    public class Parallel : BTNode
    {
        private readonly BTNode[] _children;

        public Parallel(params BTNode[] children)
        {
            _children = children;
        }

        protected override NodeStatus Process()
        {
            bool anyRunning = false;
            bool anyFailed = false;

            foreach (var child in _children)
            {
                var status = child.Evaluate();
                if (status == NodeStatus.Running) anyRunning = true;
                if (status == NodeStatus.Failure) anyFailed = true;
            }

            if (anyFailed) return Status = NodeStatus.Failure;
            if (anyRunning) return Status = NodeStatus.Running;
            return Status = NodeStatus.Success;
        }

        protected override void Exit()
        {
            foreach (var child in _children)
            {
                if (child.Status == NodeStatus.Running)
                {
                    child.Reset();
                }
            }
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
