namespace _Project.Scripts.Architecture.BehaviorTree
{
    public class BehaviourTree
    {
        private readonly BTNode _root;

        public NodeStatus LastStatus { get; private set; }
        public Blackboard Blackboard { get; private set; }

        public BehaviourTree(BTNode root)
        {
            _root = root;
            Blackboard = new Blackboard();
            PropagateBlackboard(_root);
        }

        public NodeStatus Tick()
        {
            return LastStatus = _root.Evaluate();
        }

        public void Reset()
        {
            _root.Reset();
        }

        private void PropagateBlackboard(BTNode node)
        {
            node.SetBlackboard(Blackboard);
        }
    }
}