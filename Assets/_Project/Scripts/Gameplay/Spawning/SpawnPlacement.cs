using UnityEngine;
using UnityEngine.AI;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Gameplay.Spawning
{
    public static class SpawnPlacement
    {
        private const float SampleRadius = 8f;

        // ставить персонажа через transform.position нельзя: NavMeshAgent держит свою позицию
        // и утаскивает его обратно в точку создания префаба
        public static void Place(CharacterEntity character, Vector3 position)
        {
            if (character == null)
                return;

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas))
                position = hit.position;

            var agent = character.GetComponent<NavMeshAgent>();

            if (agent != null)
                agent.Warp(position);
            else
                character.transform.position = position;
        }
    }
}
