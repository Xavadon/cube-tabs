using System;
using System.Collections.Generic;
using _Project.Scripts.Architecture.Services;
using _Project.Scripts.Architecture.Services.Scene;
using _Project.Scripts.Architecture.Services.Tick;
using _Project.Scripts.Gameplay.Services.Scene;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture
{
    public static class Project
    {
        private static DiContainer _container;
        private static bool _servicesReady;

        // именно флаг, а не проверка контейнера на null: Dispose чистит его содержимое, но ссылку оставляет
        public static bool IsInitialized => _servicesReady;

        public static async UniTask Initialize()
        {
            await InitializeServicesOnly();

            // стартовый контент выдаётся до первой сцены и ровно один раз за запуск
            _container.Resolve<IArpgStartupService>().GrantStartingContent();

            var sceneService = _container.Resolve<ISceneService>();
            await sceneService.LoadStartScene();

            // Сцена отрисована — только теперь игра считается загруженной для платформы (Yandex п.1.19).
            await UniTask.DelayFrame(2);
            PlatformSignals.GameReady();

            Debug.Log("[Project] Приложение инициализировано успешно");
        }

        public static async UniTask InitializeServicesOnly()
        {
            Dispose();
            RegisterServices();
            await InitializeServices();

            _servicesReady = true;
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
            _servicesReady = false;

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
