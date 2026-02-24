using UnityEngine;

namespace _Project.Scripts.Architecture.BehaviorTree.Decorators
{
    public class Cooldown : BTNode
    {
        private readonly BTNode _child;
        private readonly float _duration;
        private float _lastSuccessTime = float.NegativeInfinity;

        public Cooldown(float duration, BTNode child)
        {
            _duration = duration;
            _child = child;
        }

        protected override NodeStatus Process()
        {
            if (Time.time - _lastSuccessTime < _duration)
            {
                return Status = NodeStatus.Failure;
            }

            Status = _child.Evaluate();

            if (Status == NodeStatus.Success)
            {
                _lastSuccessTime = Time.time;
            }

            return Status;
        }

        public override void Reset()
        {
            base.Reset();
            _lastSuccessTime = float.NegativeInfinity;
            _child.Reset();
        }

        protected override void OnBlackboardSet()
        {
            _child.SetBlackboard(Blackboard);
        }
    }
}