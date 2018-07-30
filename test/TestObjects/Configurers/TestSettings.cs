// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Test.Common.Configurers
{
    // [Serializable]
    public class TestSettings : IDisposable
    {
        /// <summary>
        /// the configurers.
        /// </summary>
        private Collection<IConfigurer> _configurers;

        /// <sumary>
        /// Collection of configurers, all of these need to be marked // [Serializable]
        ///  This is private because you should always use the helper methods below to modify it.
        ///  The reason for this is because eventually we want to the locking mechanism 
        ///  to prevent modification of the configurers after the settings have been locked.
        ///  </sumary>
        private ICollection<IConfigurer> Configurers
        {
            get
            {
                if (_configurers == null)
                {
                    _configurers = new Collection<IConfigurer>();
                }
                return _configurers;
            }
        }


        private LockableDictionary<string, string> _properties;
        /// <summary>
        ///  Collection of deployment settings.
        ///  Its a dictionary of string, string because this means it will always be serializable.
        ///  
        /// Properties should only include settings which are shared between multiple configurers. Any settings which are unique to one configurer,
        ///  should be set directly on that configurer.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new LockableDictionary<string, string>();
                }
                return _properties;
            }
        }

        public void Lock()
        {
            _properties.Locked = true;
        }

        /// <summary>
        /// Dispose just calls dispose on any dispoable configurers.
        /// </summary>
        public void Dispose()
        {
            foreach (IConfigurer configurer in this.Configurers)
            {
                if (configurer is IDisposable)
                {
                    ((IDisposable)configurer).Dispose();
                }
            }
        }

        #region Helpers

        /// <summary>
        /// Returns the first configurer of the same type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Find<T>() where T : IConfigurer
        {
            foreach (IConfigurer config in this.Configurers)
            {
                if (config is T)
                {
                    return (T)config;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Finds a configurer that matches the T.
        //  If one doesnt exist, a new one is created and it will be automatically added to the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>() where T : IConfigurer, new()
        {
            T value = Find<T>();
            if (value == null)
            {
                this.Configurers.Add(value = new T());
            }
            return value;
        }

        /// <summary>
        /// Finds all of the configurers of a certain type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> FindAll<T>() where T : IConfigurer
        {
            List<T> results = new List<T>();
            foreach (IConfigurer config in this.Configurers)
            {
                if (config is T)
                {
                    results.Add((T)config);
                }
            }
            return results;
        }

        /// <summary>
        /// Helper to add to the collection of configurers
        /// </summary>
        /// <param name="configurer"></param>
        public void Add(IConfigurer configurer)
        {
            this.Configurers.Add(configurer);
        }

        /// <summary>
        /// Helper to add to the colelction of configurers
        /// 
        ///  The generic parameter isnt needed anymore, the method is only here for back compat reasons.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        public void Add<T>(T item) where T : IConfigurer
        {
            this.Configurers.Add(item);
        }

        /// <summary>
        /// Remove all of the configurers of a certain type.
        /// </summary>
        public void Remove<T>() where T : IConfigurer
        {
            foreach (T item in this.FindAll<T>())
            {
                this.Configurers.Remove(item);
            }
        }

        /// <summary>
        /// Finds all configurers of type T, and then runs them 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        public void RunConfigurers<T>(T target)
        {
            foreach (IConfigurer<T> configurer in this.FindAll<IConfigurer<T>>())
            {
                configurer.Configure(target, this);
            }
        }

        #endregion

        /// <summary>
        /// Clones this.
        /// </summary>
        /// <returns>The Clone</returns>
        public object Clone()
        {
            TestSettings settings = new TestSettings();
            foreach (string property in this.Properties.Keys)
            {
                settings.Properties.Add(property, this.Properties[property]);
            }

            foreach (IConfigurer configurer in this.Configurers)
            {
                settings.Add(configurer);
            }
            return settings;
        }

        // [Serializable]
        private class LockableDictionary<K, V> : IDictionary<K, V>
        {
            public bool Locked { get; set; }
            private IDictionary<K, V> _dic = new Dictionary<K, V>();

            public void Add(K key, V value)
            {
                ThrowIfLocked();
                _dic.Add(key, value);
            }

            public bool Remove(K key)
            {
                ThrowIfLocked();
                return _dic.Remove(key);
            }

            public bool TryGetValue(K key, out V value)
            {
                return _dic.TryGetValue(key, out value);
            }

            public ICollection<V> Values
            {
                get { return _dic.Values; }
            }

            public V this[K key]
            {
                get
                {
                    return _dic[key];
                }
                set
                {
                    ThrowIfLocked();
                    _dic[key] = value;
                }
            }

            public void Add(KeyValuePair<K, V> item)
            {
                ThrowIfLocked();
                _dic.Add(item);
            }

            public void Clear()
            {
                ThrowIfLocked();
                _dic.Clear();
            }

            public bool Contains(KeyValuePair<K, V> item)
            {
                return _dic.Contains(item);
            }

            public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
            {
                _dic.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return _dic.Count; }
            }

            public bool IsReadOnly
            {
                get { return _dic.IsReadOnly; }
            }

            public bool Remove(KeyValuePair<K, V> item)
            {
                ThrowIfLocked();
                return _dic.Remove(item);
            }

            public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                return _dic.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _dic.GetEnumerator();
            }

            public bool ContainsKey(K key)
            {
                return _dic.ContainsKey(key);
            }

            public ICollection<K> Keys
            {
                get { return _dic.Keys; }
            }

            private void ThrowIfLocked()
            {
                if (this.Locked)
                {
                    throw new InvalidOperationException("Dictionary is locked.");
                }
            }
        }
    }
}
