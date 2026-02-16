using System.Collections.Generic;

namespace _Project.Scripts.Architecture.BehaviorTree
{
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new();

        public void Set<T>(string key, T value)
        {
            _data[key] = value;
        }

        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var objValue) && objValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public bool Has(string key)
        {
            return _data.ContainsKey(key);
        }

        public void Remove(string key)
        {
            _data.Remove(key);
        }

        public void Clear()
        {
            _data.Clear();
        }
    }
}
