using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using IDisposable = _Project.Scripts.Architecture.Services.IDisposable;

namespace _Project.Scripts.Architecture.Services
{
    public class DiContainer
    {
        private readonly Dictionary<Type, Type> _registrations = new();
        private readonly Dictionary<Type, object> _singletons = new();
        private readonly Dictionary<Type, object> _transients = new();
        
        public void RegisterInstance<TInterface, TImplementation>()
            where TImplementation : TInterface
        {
            var interfaceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);

            if (_registrations.ContainsKey(interfaceType))
            {
                Debug.LogWarning($"[DIContainer] Service {interfaceType.Name} already registered.");
            }

            _registrations[interfaceType] = implementationType;
            Debug.Log($"[DIContainer] register instance: {interfaceType.Name} -> {implementationType.Name}");
        }

        public void RegisterAllFromAssembly()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var serviceTypes = assembly.GetTypes()
                .Where(t => typeof(IService).IsAssignableFrom(t)
                         && t.IsClass
                         && !t.IsAbstract)
                .ToList();

            Debug.Log($"[DIContainer] Найдено {serviceTypes.Count} сервисов для автоматической регистрации");

            foreach (var serviceType in serviceTypes)
            {
                var serviceInterface = GetServiceInterface(serviceType);

                // Используем рефлексию для вызова generic метода RegisterInstance
                var registerMethod = typeof(DiContainer)
                    .GetMethod(nameof(RegisterInstance))
                    .MakeGenericMethod(serviceInterface, serviceType);

                registerMethod.Invoke(this, null);
            }

            Debug.Log("[DIContainer] Все сервисы автоматически зарегистрированы");
        }

        private Type GetServiceInterface(Type serviceType)
        {
            // Получаем все интерфейсы, исключая базовые системные
            var interfaces = serviceType.GetInterfaces()
                .Where(i => i != typeof(IService)
                         && i != typeof(IDisposable)
                         && !i.IsGenericType
                         && i.Namespace != null
                         && !i.Namespace.StartsWith("System"))
                .ToList();

            // Если есть специфичный интерфейс - используем самый специфичный
            if (interfaces.Count > 0)
            {
                // Берем интерфейс с наименьшим количеством базовых интерфейсов (самый конкретный)
                return interfaces
                    .OrderBy(i => i.GetInterfaces().Length)
                    .First();
            }

            // Иначе регистрируем класс сам на себя
            return serviceType;
        }
        
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }
        
        private object Resolve(Type type)
        {
            if (_singletons.TryGetValue(type, out var singletonInstance))
            {
                return singletonInstance;
            }

            Type implementationType;
            if (_registrations.TryGetValue(type, out var registeredType))
            {
                implementationType = registeredType;
            }
            else if (!type.IsAbstract && !type.IsInterface)
            {
                implementationType = type;
            }
            else
            {
                throw new Exception($"[DIContainer] Тип {type.Name} не зарегистрирован в контейнере!");
            }

            var constructors = implementationType.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new Exception($"[DIContainer] У типа {implementationType.Name} нет публичных конструкторов!");
            }

            var constructor = constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            var parameters = constructor.GetParameters();
            var parameterInstances = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;

                try
                {
                    parameterInstances[i] = Resolve(parameterType);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"[DIContainer] Не удалось резолвить зависимость '{parameterType.Name}' " +
                        $"для конструктора '{implementationType.Name}'. " +
                        $"Убедитесь что зависимость зарегистрирована в контейнере.", ex);
                }
            }

            var instance = Activator.CreateInstance(implementationType, parameterInstances);
            _singletons[type] = instance;

            Debug.Log($"[DIContainer] Создан инстанс: {type.Name} (реализация: {implementationType.Name})");

            return instance;
        }
        
        public List<T> ResolveAll<T>() where T : class
        {
            var result = new List<T>();
            var targetType = typeof(T);

            foreach (var service in _singletons.Values)
            {
                if (targetType.IsAssignableFrom(service.GetType()))
                {
                    result.Add(service as T);
                }
            }

            return result;
        }
        
        public async UniTask InitializeAllServices()
        {
            Debug.Log("[DIContainer] Начало инициализации всех сервисов...");

            foreach (var registration in _registrations.ToList())
            {
                try
                {
                    Resolve(registration.Key);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DIContainer] Ошибка при создании сервиса {registration.Key.Name}: {ex.Message}");
                    throw;
                }
            }

            var services = _singletons.Values
                .OfType<IService>()
                .ToList();

            foreach (var service in services)
            {
                try
                {
                    await service.Initialize();
                    Debug.Log($"[DIContainer] Сервис {service.GetType().Name} инициализирован.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DIContainer] Ошибка при инициализации {service.GetType().Name}: {ex.Message}");
                    throw;
                }
            }

            Debug.Log($"[DIContainer] Все сервисы инициализированы ({services.Count} шт.)");
        }
        
        public void Clear()
        {
            Debug.Log("[DIContainer] Очистка контейнера...");

            foreach (var instance in _singletons.Values.OfType<IDisposable>())
            {
                try
                {
                    instance.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DIContainer] Ошибка при Dispose сервиса {instance.GetType().Name}: {ex.Message}");
                }
            }

            _registrations.Clear();
            _singletons.Clear();
            _transients.Clear();

            Debug.Log("[DIContainer] Контейнер очищен.");
        }
    }
}
