namespace _Project.Scripts.Architecture.BehaviorTree
{
    public enum NodeStatus
    {
        Running,
        Success,
        Failure,
        Locked
    }

    public abstract class BTNode
    {
        public NodeStatus Status { get; protected set; }
        protected Blackboard Blackboard { get; private set; }

        private bool _isActive;

        public virtual NodeStatus Evaluate()
        {
            if (!_isActive)
            {
                _isActive = true;
                Enter();
            }

            Status = Process();

            if (Status != NodeStatus.Running)
            {
                _isActive = false;
                Exit();
            }

            return Status;
        }

        protected abstract NodeStatus Process();

        protected virtual void Enter() { }

        protected virtual void Exit() { }

        public virtual void Reset()
        {
            if (_isActive)
            {
                _isActive = false;
                Exit();
            }
            Status = NodeStatus.Failure;
        }

        public void SetBlackboard(Blackboard blackboard)
        {
            Blackboard = blackboard;
            OnBlackboardSet();
        }

        protected virtual void OnBlackboardSet()
        {
        }
    }
}
