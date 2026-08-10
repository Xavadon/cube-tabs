using _Project.Scripts.Gameplay.Spawning;
using UnityEngine;
using UnityEngine.AI;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    // переход между этапами внутри одной сцены: этапы стоят далеко друг от друга, игрока переносим
    public class StagePortal : Portal
    {
        [SerializeField] private Transform _target;
        [SerializeField] private DungeonStage _targetStage;

        protected override bool OneShot => false;

        protected override void Use(CharacterEntity player)
        {
            if (_target == null)
                return;

            var agent = player.GetComponent<NavMeshAgent>();

            if (agent != null)
                agent.Warp(_target.position);
            else
                player.transform.position = _target.position;

            if (_targetStage != null)
                _targetStage.Begin();
        }
    }
}
