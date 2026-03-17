using System;
using System.Collections.Generic;

namespace _Project.Scripts.Architecture
{
    public class Pool<T> where T : class
    {
        private readonly Stack<T> _stack;
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onReturn;

        public int CountInactive => _stack.Count;

        public Pool(Func<T> createFunc, Action<T> onGet = null, Action<T> onReturn = null, int preloadCount = 0)
        {
            _createFunc = createFunc;
            _onGet = onGet;
            _onReturn = onReturn;
            _stack = new Stack<T>(preloadCount > 0 ? preloadCount : 8);

            for (int i = 0; i < preloadCount; i++)
                _stack.Push(_createFunc());
        }

        public T Get()
        {
            T item = _stack.Count > 0 ? _stack.Pop() : _createFunc();
            _onGet?.Invoke(item);
            return item;
        }

        public void Return(T item)
        {
            _onReturn?.Invoke(item);
            _stack.Push(item);
        }
    }
}
