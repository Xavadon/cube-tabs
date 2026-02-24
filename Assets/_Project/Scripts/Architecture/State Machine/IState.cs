using UnityEditor.Experimental.GraphView;

namespace _Project.Scripts.Architecture.State_Machine
{
    public interface IBlackboardState
    {
        void RegisterBlackboard(Blackboard blackboard);
    }
    
    public interface IState : IExitableState
    {
        void Enter();
    }

    public interface IState<TArg1> : IExitableState
    {
        void Enter(TArg1 arg1);
    }

    public interface IState<TArg1, TArg2> : IExitableState
    {
        void Enter(TArg1 arg1, TArg2 arg2);
    }

    public interface IState<TArg1, TArg2, TArg3> : IExitableState
    {
        void Enter(TArg1 arg1, TArg2 arg2, TArg3 arg3);
    }
}