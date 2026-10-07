using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Audio
{
    public class MusicShuffleBag
    {
        private readonly int[] _order;
        private int _cursor;
        private int _lastPlayed = -1;

        public MusicShuffleBag(int count)
        {
            _order = new int[count];
            for (int i = 0; i < count; i++)
            {
                _order[i] = i;
            }

            _cursor = count;
        }

        public int Next()
        {
            if (_cursor >= _order.Length)
            {
                Reshuffle();
            }

            _lastPlayed = _order[_cursor];
            _cursor++;
            return _lastPlayed;
        }

        private void Reshuffle()
        {
            for (int i = _order.Length - 1; i > 0; i--)
            {
                Swap(i, Random.Range(0, i + 1));
            }

            if (_order.Length > 1 && _order[0] == _lastPlayed)
            {
                Swap(0, Random.Range(1, _order.Length));
            }

            _cursor = 0;
        }

        private void Swap(int a, int b)
        {
            int temp = _order[a];
            _order[a] = _order[b];
            _order[b] = temp;
        }
    }
}
