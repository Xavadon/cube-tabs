namespace _Project.Scripts.Architecture.BehaviorTree
{
    public enum NodeStatus
    {
        Running,
        Success,
        Failure
    }
    
    public abstract class BTNode
    {
        public NodeStatus Status { get; protected set; }
        protected Blackboard Blackboard { get; private set; }

        public abstract NodeStatus Evaluate();

        public virtual void Reset()
        {
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