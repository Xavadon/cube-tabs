namespace _Project.Scripts.Architecture.BehaviorTree.Decorators
{
    public class Inverter : BTNode
    {
        private readonly BTNode _child;

        public Inverter(BTNode child)
        {
            _child = child;
        }

        public override NodeStatus Evaluate()
        {
            return Status = _child.Evaluate() switch
            {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => NodeStatus.Running
            };
        }

        public override void Reset()
        {
            base.Reset();
            _child.Reset();
        }

        protected override void OnBlackboardSet()
        {
            _child.SetBlackboard(Blackboard);
        }
    }
}