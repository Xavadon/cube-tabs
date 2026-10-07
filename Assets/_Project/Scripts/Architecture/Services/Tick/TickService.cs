using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Tick
{
    public class TickService : ITickService
    {
        private readonly List<ITickable> _tickables = new();
        private TickBehaviour _tickBehaviour;

        public UniTask Initialize()
        {
            _tickBehaviour = Project.CreateTickBehaviour();
            _tickBehaviour.Initialize(Project.GetAll<ITickable>());
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (_tickBehaviour != null)
            {
                Object.Destroy(_tickBehaviour.gameObject);
                _tickBehaviour = null;
            }
        }

        public void Register(ITickable tickable)
        {
            _tickBehaviour.Register(tickable);
        }

        public void Unregister(ITickable tickable)
        {
            _tickBehaviour.Unregister(tickable);
        }
    }
    
    public class TickBehaviour : MonoBehaviour
    {
        private List<ITickable> _tickables;
        
        public void Initialize(List<ITickable> tickables)
        {
            _tickables = tickables;
        }
        
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            foreach (var tickable in _tickables)
            {
                tickable.Tick(deltaTime);
            }
        }

        private void OnDisable()
        {
            _tickables.Clear();
        }

        public void Register(ITickable tickable)
        {
            _tickables.Add(tickable);
        }
        
        public void Unregister(ITickable tickable)
        {
            if (_tickables.Contains(tickable))
            {
                _tickables.Remove(tickable);
            }
        }
    }
}