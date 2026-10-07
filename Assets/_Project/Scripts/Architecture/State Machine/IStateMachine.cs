namespace _Project.Scripts.Architecture.State_Machine
{
    public interface IStateMachine
    {
        void Enter<TState>() where TState : class, IState;
        void Enter<TState, TArg1>(TArg1 arg1) where TState : class, IState<TArg1>;
        void Enter<TState, TArg1, TArg2>(TArg1 arg1, TArg2 arg2) where TState : class, IState<TArg1, TArg2>;
        void Enter<TState, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3) where TState : class, IState<TArg1, TArg2, TArg3>;
    }
}
