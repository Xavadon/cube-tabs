using _Project.Scripts.Architecture.BehaviorTree;

namespace _Project.Scripts.Gameplay.Character.Data.AiBrain
{
    public interface IBrainData
    {
        BTNode BuildTree();
    }
}