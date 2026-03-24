using _Project.Scripts.Architecture.BehaviorTree;
using _Project.Scripts.Gameplay.Character.Data;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    public abstract class BrainDataBase : ScriptableObject
    {
        [field: SerializeField]
        public FindTargetType FindTargetType { get; private set; } = FindTargetType.Nearest;

        public abstract BTNode BuildTree(LayerMask targetLayer, TierData tier);
    }
}