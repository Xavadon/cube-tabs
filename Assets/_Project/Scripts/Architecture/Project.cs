using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Architecture.Services.Tick;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture
{
    public static class Project
    {
        private static DiContainer _container;
        
        public static async UniTask Initialize()
        {
            Dispose();
            RegisterServices();
            await InitializeServices();
            
            var sceneService = _container.Resolve<SceneService>();
            await sceneService.LoadMenuScene();

            Debug.Log("[Project] Приложение инициализировано успешно");
        }
        
        private static void RegisterServices()
        {
            if (_container == null)
            {
                _container = new DiContainer();
                Debug.Log("[Project] DI контейнер создан.");
            }

            // Автоматическая регистрация всех сервисов из сборки
            _container.RegisterAllFromAssembly();
        }
        
        private static async UniTask InitializeServices()
        {
            if (_container == null)
            {
                throw new Exception("[Project] DI контейнер не инициализирован!");
            }

            await _container.InitializeAllServices();
        }

        public static T Get<T>() where T : class
        {
            if (_container == null)
            {
                throw new Exception("[Project] DI контейнер не инициализирован!");
            }

            return _container.Resolve<T>();
        }
        
        //TODO: найти способ избавиться от такого метода и стоит ли вообще
        public static List<T> GetAll<T>() where T : class
        {
            if (_container == null)
            {
                throw new Exception("[Project] DI контейнер не инициализирован!");
            }

            return _container.ResolveAll<T>();
        }
        
        public static void Dispose()
        {
            if (_container != null)
            {
                _container.Clear();
                Debug.Log("[Project] DI контейнер очищен.");
            }
        }
        
        //TODO: create factory
        public static TickBehaviour CreateTickBehaviour()
        {
            var obj = new GameObject("TickBehaviour");
            UnityEngine.Object.DontDestroyOnLoad(obj);
            var tickBehaviour = obj.AddComponent<TickBehaviour>();
            Debug.Log("[Project] TickBehaviour создан");
            return tickBehaviour;
        }
    }
}
