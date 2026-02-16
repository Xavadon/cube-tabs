using UnityEngine;

namespace _Project.Scripts.Architecture.BehaviorTree.Leaves
{
    public class Wait : BTNode
    {
        private readonly float _duration;
        private float _elapsed;
        private bool _isWaiting;

        public Wait(float duration)
        {
            _duration = duration;
        }

        public override NodeStatus Evaluate()
        {
            if (!_isWaiting)
            {
                _isWaiting = true;
                _elapsed = 0f;
            }

            _elapsed += Time.deltaTime;

            if (_elapsed >= _duration)
            {
                _isWaiting = false;
                return Status = NodeStatus.Success;
            }

            return Status = NodeStatus.Running;
        }

        public override void Reset()
        {
            base.Reset();
            _isWaiting = false;
            _elapsed = 0f;
        }
    }
}