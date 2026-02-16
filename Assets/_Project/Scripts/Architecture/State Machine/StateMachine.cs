using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using _Project.Scripts.Architecture.Services.Tick;

namespace _Project.Scripts.Architecture.State_Machine
{
    public abstract class StateMachine : IStateMachine
    {
        private readonly Dictionary<Type, IExitableState> _registeredStatesMap = new();
        private readonly Dictionary<Type, Func<bool>> _transitionsMap = new();

        private Type _currentStateType;
        private IExitableState _currentState;

        public string CurrentStateName => _currentStateType?.Name ?? "None";

        public void Enter<TState>() where TState : class, IState
        {
            var stateType = typeof(TState);

            if (IsSameState(stateType))
                return;

            var newState = ChangeState(stateType) as IState;
            newState.Enter();
        }

        public void Enter<TState, TArg1>(TArg1 arg1) where TState : class, IState<TArg1>
        {
            var stateType = typeof(TState);

            if (IsSameState(stateType))
                return;

            var newState = ChangeState(stateType) as TState;
            newState.Enter(arg1);
        }

        public void Enter<TState, TArg1, TArg2>(TArg1 arg1, TArg2 arg2) 
            where TState : class, IState<TArg1, TArg2>
        {
            var stateType = typeof(TState);

            if (IsSameState(stateType))
                return;

            var newState = ChangeState(stateType) as TState;
            newState.Enter(arg1, arg2);
        }

        public void Enter<TState, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3) 
            where TState : class, IState<TArg1, TArg2, TArg3>
        {
            var stateType = typeof(TState);

            if (IsSameState(stateType))
                return;

            var newState = ChangeState(stateType) as TState;
            newState.Enter(arg1, arg2, arg3);
        }
        
        public void Dispose()
        {
            _registeredStatesMap.Clear();
            _transitionsMap.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UpdateCurrentState(float deltaTime)
        {
            if (_currentState is ITickable tickable)
                tickable.Tick(deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UpdateTransitions()
        {
            foreach (var transitionMap in _transitionsMap)
            {
                if (transitionMap.Value.Invoke() && !IsSameState(transitionMap.Key))
                    Enter(transitionMap.Key);
            }
        }

        protected void RegisterState<TState>(TState state) where TState : IExitableState =>
            _registeredStatesMap.Add(typeof(TState), state);

        protected void RegisterTransition<TState>(Func<bool> condition) where TState : IExitableState =>
            _transitionsMap.Add(typeof(TState), condition);

        private IExitableState ChangeState(Type stateType)
        {
            _currentState?.Exit();

            var state = GetState(stateType);
            _currentState = state;
            _currentStateType = stateType;

            return state;
        }

        private void Enter(Type state)
        {
            var newState = ChangeState(state) as IState;
            newState.Enter();
        }

        private bool IsSameState(Type stateType) => _currentStateType != null && _currentStateType == stateType;

        private IExitableState GetState(Type type) => _registeredStatesMap[type];
    }
}