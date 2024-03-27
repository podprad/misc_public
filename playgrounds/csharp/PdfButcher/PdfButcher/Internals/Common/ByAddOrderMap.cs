namespace PdfButcher.Internals.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class ByAddOrderMap<TKey, TValue> : IEnumerable<TValue>
    {
        private readonly Dictionary<TKey, ValueWrapper> _map = new Dictionary<TKey, ValueWrapper>();

        private int _counter = 0;

        /// <summary>
        /// Use only if need values in order.
        /// </summary>
        public IEnumerable<TValue> ValuesInOrder => _map.Values.OrderBy(g => g.Index).Select(g => g.Value);

        /// <summary>
        /// Faster.
        /// </summary>
        public IEnumerable<TValue> Values => _map.Values.Select(g => g.Value);

        /// <summary>
        /// Changes keys of elements. Input is dictionary of (old key) => (new key)
        /// </summary>
        public void ChangeKeys(Dictionary<TKey, TKey> changes)
        {
            foreach (var currentId in _map.Keys)
            {
                if (!changes.ContainsKey(currentId))
                {
                    throw new InvalidOperationException($"Missing change for key {currentId}");
                }
            }

            var backup = new Dictionary<TKey, ValueWrapper>();

            foreach (var oldId in changes.Keys)
            {
                backup[oldId] = _map[oldId];
                _map.Remove(oldId);
            }

            foreach (var pair in changes)
            {
                var oldId = pair.Key;
                var newId = pair.Value;

                var wrapper = backup[oldId];
                _map[newId] = wrapper;
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (!_map.ContainsKey(key))
            {
                _map.Add(key, new ValueWrapper(value, _counter++));
            }
        }

        public void Remove(TKey key)
        {
            _map.Remove(key);
        }

        public bool ContainsKey(TKey key)
        {
            return _map.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_map.TryGetValue(key, out var wrapper))
            {
                value = wrapper.Value;

                return true;
            }

            value = default;

            return false;
        }

        public TValue GetValueOrThrow(TKey key)
        {
            return _map[key].Value;
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            return ValuesInOrder.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [DebuggerDisplay("{" + nameof(Display) + "}")]
        private class ValueWrapper
        {
            public ValueWrapper(TValue value, int index)
            {
                Value = value;
                Index = index;
            }

            public TValue Value { get; }

            public int Index { get; }

            public string Display => $"{Value} at index {Index}";
        }
    }
}