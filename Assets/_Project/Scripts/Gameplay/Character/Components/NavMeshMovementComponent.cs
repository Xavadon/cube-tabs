using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Core.Gameplay.Enemies.Components
{
    public class NavMeshMovementComponent 
    {
        private readonly NavMeshAgent _agent;
        private readonly Transform _transform;
        private readonly float _baseSpeed;

        public NavMeshMovementComponent(NavMeshAgent agent, Transform transform, float speed)
        {
            _agent = agent;
            _transform = transform;
            _baseSpeed = speed;

            _agent.speed = speed;
            _agent.angularSpeed = 360f;
            _agent.acceleration = 8f;
        }

        public void MoveTo(Vector3 destination)
        {
            if (!_agent.isActiveAndEnabled)
            {
                Debug.LogError($"[MovementComponent] NavMeshAgent не активен на {_transform.name}");
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                Debug.LogError($"[MovementComponent] {_transform.name} НЕ НА NavMesh! Position: {_transform.position}");
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        public void MoveInDirection(Vector3 direction)
        {
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            {
                return;
            }

            _agent.isStopped = false;
            Vector3 targetPos = _transform.position + direction.normalized * 2f;
            _agent.SetDestination(targetPos);
        }

        public void Stop()
        {
            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
        }

        public void LookAt(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - _transform.position).normalized;
            direction.y = 0; // Только поворот по Y

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

        public float GetPathDistance(Vector3 destination)
        {
            if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
            {
                return Vector3.Distance(_transform.position, destination);
            }

            NavMeshPath path = new NavMeshPath();
            if (_agent.CalculatePath(destination, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                float distance = 0f;
                for (int i = 0; i < path.corners.Length - 1; i++)
                {
                    distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                }
                return distance;
            }

            return Vector3.Distance(_transform.position, destination);
        }

        public void SetSpeed(float speed)
        {
            if (_agent != null)
            {
                _agent.speed = speed;
            }
        }

        public void ResetSpeed()
        {
            SetSpeed(_baseSpeed);
        }
    }
}
