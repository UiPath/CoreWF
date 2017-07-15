// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CoreWf
{
    internal class HybridDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        private TKey _singleItemKey;
        private TValue _singleItemValue;
        private IDictionary<TKey, TValue> _dictionary;

        public int Count
        {
            get
            {
                if (_singleItemKey != null)
                {
                    return 1;
                }
                else if (_dictionary != null)
                {
                    return _dictionary.Count;
                }

                return 0;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (_singleItemKey != null)
                {
                    return new ReadOnlyCollection<TValue>(new List<TValue>() { _singleItemValue });
                }
                else if (_dictionary != null)
                {
                    return new ReadOnlyCollection<TValue>(new List<TValue>(_dictionary.Values));
                }

                return null;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (_singleItemKey != null)
                {
                    return new ReadOnlyCollection<TKey>(new List<TKey>() { _singleItemKey });
                }
                else if (_dictionary != null)
                {
                    return new ReadOnlyCollection<TKey>(new List<TKey>(_dictionary.Keys));
                }

                return null;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (_singleItemKey == key)
                {
                    return _singleItemValue;
                }
                else if (_dictionary != null)
                {
                    return _dictionary[key];
                }

                return null;
            }

            set
            {
                this.Add(key, value);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("key");
            }

            if (_singleItemKey == null && _singleItemValue == null && _dictionary == null)
            {
                _singleItemKey = key;
                _singleItemValue = value;
            }
            else if (_singleItemKey != null)
            {
                _dictionary = new Dictionary<TKey, TValue>();

                _dictionary.Add(_singleItemKey, _singleItemValue);

                _singleItemKey = null;
                _singleItemValue = null;

                _dictionary.Add(key, value);
                return;
            }
            else
            {
                Fx.Assert(_dictionary != null, "We should always have a dictionary at this point");

                _dictionary.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null)
            {
                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("key");
            }

            if (_singleItemKey != null)
            {
                return _singleItemKey == key;
            }
            else if (_dictionary != null)
            {
                return _dictionary.ContainsKey(key);
            }

            return false;
        }

        public bool Remove(TKey key)
        {
            if (_singleItemKey == key)
            {
                _singleItemKey = null;
                _singleItemValue = null;

                return true;
            }
            else if (_dictionary != null)
            {
                bool ret = _dictionary.Remove(key);

                if (_dictionary.Count == 0)
                {
                    _dictionary = null;
                }

                return ret;
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_singleItemKey == key)
            {
                value = _singleItemValue;
                return true;
            }
            else if (_dictionary != null)
            {
                return _dictionary.TryGetValue(key, out value);
            }

            value = null;
            return false;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _singleItemKey = null;
            _singleItemValue = null;
            _dictionary = null;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (_singleItemKey != null)
            {
                return _singleItemKey == item.Key && _singleItemValue == item.Value;
            }
            else if (_dictionary != null)
            {
                return _dictionary.Contains(item);
            }

            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (_singleItemKey != null)
            {
                array[arrayIndex] = new KeyValuePair<TKey, TValue>(_singleItemKey, _singleItemValue);
            }
            else if (_dictionary != null)
            {
                _dictionary.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return this.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (_singleItemKey != null)
            {
                yield return new KeyValuePair<TKey, TValue>(_singleItemKey, _singleItemValue);
            }
            else if (_dictionary != null)
            {
                foreach (KeyValuePair<TKey, TValue> kvp in _dictionary)
                {
                    yield return kvp;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
