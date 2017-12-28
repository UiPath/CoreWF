// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreWf.Runtime.Collections
{
    internal class NullableKeyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private bool _isNullKeyPresent;
        private TValue _nullKeyValue;
        private IDictionary<TKey, TValue> _innerDictionary;

        public NullableKeyDictionary()
            : base()
        {
            _innerDictionary = new Dictionary<TKey, TValue>();
        }

        public int Count
        {
            get { return _innerDictionary.Count + (_isNullKeyPresent ? 1 : 0); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return new NullKeyDictionaryKeyCollection<TKey, TValue>(this);
            }
        }

        public ICollection<TValue> Values
        {
            get { return new NullKeyDictionaryValueCollection<TKey, TValue>(this); }
        }

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                {
                    if (_isNullKeyPresent)
                    {
                        return _nullKeyValue;
                    }
                    else
                    {
                        throw Fx.Exception.AsError(new KeyNotFoundException());
                    }
                }
                else
                {
                    return _innerDictionary[key];
                }
            }
            set
            {
                if (key == null)
                {
                    _isNullKeyPresent = true;
                    _nullKeyValue = value;
                }
                else
                {
                    _innerDictionary[key] = value;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                if (_isNullKeyPresent)
                {
                    throw Fx.Exception.Argument("key", SR.NullKeyAlreadyPresent);
                }
                _isNullKeyPresent = true;
                _nullKeyValue = value;
            }
            else
            {
                _innerDictionary.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            return key == null ? _isNullKeyPresent : _innerDictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                bool result = _isNullKeyPresent;
                _isNullKeyPresent = false;
                _nullKeyValue = default(TValue);
                return result;
            }
            else
            {
                return _innerDictionary.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                if (_isNullKeyPresent)
                {
                    value = _nullKeyValue;
                    return true;
                }
                else
                {
                    value = default(TValue);
                    return false;
                }
            }
            else
            {
                return _innerDictionary.TryGetValue(key, out value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _isNullKeyPresent = false;
            _nullKeyValue = default(TValue);
            _innerDictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null)
            {
                if (_isNullKeyPresent)
                {
                    return item.Value == null ? _nullKeyValue == null : item.Value.Equals(_nullKeyValue);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return _innerDictionary.Contains(item);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _innerDictionary.CopyTo(array, arrayIndex);
            if (_isNullKeyPresent)
            {
                array[arrayIndex + _innerDictionary.Count] = new KeyValuePair<TKey, TValue>(default(TKey), _nullKeyValue);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key == null)
            {
                if (this.Contains(item))
                {
                    _isNullKeyPresent = false;
                    _nullKeyValue = default(TValue);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return _innerDictionary.Remove(item);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            IEnumerator<KeyValuePair<TKey, TValue>> innerEnumerator = _innerDictionary.GetEnumerator() as IEnumerator<KeyValuePair<TKey, TValue>>;

            while (innerEnumerator.MoveNext())
            {
                yield return innerEnumerator.Current;
            }

            if (_isNullKeyPresent)
            {
                yield return new KeyValuePair<TKey, TValue>(default(TKey), _nullKeyValue);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();
        }

        private class NullKeyDictionaryKeyCollection<TypeKey, TypeValue> : ICollection<TypeKey>
        {
            private NullableKeyDictionary<TypeKey, TypeValue> _nullKeyDictionary;

            public NullKeyDictionaryKeyCollection(NullableKeyDictionary<TypeKey, TypeValue> nullKeyDictionary)
            {
                _nullKeyDictionary = nullKeyDictionary;
            }

            public int Count
            {
                get
                {
                    int count = _nullKeyDictionary._innerDictionary.Keys.Count;
                    if (_nullKeyDictionary._isNullKeyPresent)
                    {
                        count++;
                    }
                    return count;
                }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public void Add(TypeKey item)
            {
                throw Fx.Exception.AsError(new NotSupportedException(SR.KeyCollectionUpdatesNotAllowed));
            }

            public void Clear()
            {
                throw Fx.Exception.AsError(new NotSupportedException(SR.KeyCollectionUpdatesNotAllowed));
            }

            public bool Contains(TypeKey item)
            {
                return item == null ? _nullKeyDictionary._isNullKeyPresent : _nullKeyDictionary._innerDictionary.Keys.Contains(item);
            }

            public void CopyTo(TypeKey[] array, int arrayIndex)
            {
                _nullKeyDictionary._innerDictionary.Keys.CopyTo(array, arrayIndex);
                if (_nullKeyDictionary._isNullKeyPresent)
                {
                    array[arrayIndex + _nullKeyDictionary._innerDictionary.Keys.Count] = default(TypeKey);
                }
            }

            public bool Remove(TypeKey item)
            {
                throw Fx.Exception.AsError(new NotSupportedException(SR.KeyCollectionUpdatesNotAllowed));
            }

            public IEnumerator<TypeKey> GetEnumerator()
            {
                foreach (TypeKey item in _nullKeyDictionary._innerDictionary.Keys)
                {
                    yield return item;
                }

                if (_nullKeyDictionary._isNullKeyPresent)
                {
                    yield return default(TypeKey);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<TypeKey>)this).GetEnumerator();
            }
        }

        private class NullKeyDictionaryValueCollection<TypeKey, TypeValue> : ICollection<TypeValue>
        {
            private NullableKeyDictionary<TypeKey, TypeValue> _nullKeyDictionary;

            public NullKeyDictionaryValueCollection(NullableKeyDictionary<TypeKey, TypeValue> nullKeyDictionary)
            {
                _nullKeyDictionary = nullKeyDictionary;
            }

            public int Count
            {
                get
                {
                    int count = _nullKeyDictionary._innerDictionary.Values.Count;
                    if (_nullKeyDictionary._isNullKeyPresent)
                    {
                        count++;
                    }
                    return count;
                }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public void Add(TypeValue item)
            {
                throw Fx.Exception.AsError(new NotSupportedException(SR.ValueCollectionUpdatesNotAllowed));
            }

            public void Clear()
            {
                throw Fx.Exception.AsError(new NotSupportedException(SR.ValueCollectionUpdatesNotAllowed));
            }

            public bool Contains(TypeValue item)
            {
                return _nullKeyDictionary._innerDictionary.Values.Contains(item) ||
                    (_nullKeyDictionary._isNullKeyPresent && _nullKeyDictionary._nullKeyValue.Equals(item));
            }

            public void CopyTo(TypeValue[] array, int arrayIndex)
            {
                _nullKeyDictionary._innerDictionary.Values.CopyTo(array, arrayIndex);
                if (_nullKeyDictionary._isNullKeyPresent)
                {
                    array[arrayIndex + _nullKeyDictionary._innerDictionary.Values.Count] = _nullKeyDictionary._nullKeyValue;
                }
            }

            public bool Remove(TypeValue item)
            {
                throw Fx.Exception.AsError(new NotSupportedException(SR.ValueCollectionUpdatesNotAllowed));
            }

            public IEnumerator<TypeValue> GetEnumerator()
            {
                foreach (TypeValue item in _nullKeyDictionary._innerDictionary.Values)
                {
                    yield return item;
                }

                if (_nullKeyDictionary._isNullKeyPresent)
                {
                    yield return _nullKeyDictionary._nullKeyValue;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<TypeValue>)this).GetEnumerator();
            }
        }
    }
}
