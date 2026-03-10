using _Project.Scripts.Architecture.BehaviorTree;
using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    public abstract class BrainDataBase : ScriptableObject
    {
        public abstract BTNode BuildTree(LayerMask targetLayer, TierData tier);
    }
}