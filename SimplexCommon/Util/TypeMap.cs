using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace Simplex.Util
{
    public class TypeMap<TKey> : IEnumerable<KeyValuePair<TKey, Type>>
    {
        private Dictionary<TKey, Type> _typeMap = new Dictionary<TKey, Type>();

        public void Add(TKey key, Type type) => _typeMap.Add(key, type);

        public Type TypeByKey(TKey key)
        {
            if (_typeMap.TryGetValue(key, out var type))
                return type;
            throw new InvalidOperationException($"Key {key} was not present in the collection");
        }

        public TKey KeyByType<TType>()
        {
            return KeyByType(typeof(TType));
        }

        public TKey KeyByType(Type type)
        {
            foreach (var m in _typeMap)
                if (m.Value == type)
                    return m.Key;
            throw new InvalidOperationException($"Type {type} was not present in the collection");
        }

        public IEnumerator<KeyValuePair<TKey, Type>> GetEnumerator()
        {
            return _typeMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _typeMap.GetEnumerator();
        }
    }
}
