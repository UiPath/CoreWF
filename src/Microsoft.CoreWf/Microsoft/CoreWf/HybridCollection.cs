// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf
{
    [DataContract]
    // used internally for performance in cases where a common usage pattern is a single item
    internal class HybridCollection<T>
            where T : class
    {
        private List<T> _multipleItems;
        private T _singleItem;

        public HybridCollection()
        {
        }

        public HybridCollection(T initialItem)
        {
            Fx.Assert(initialItem != null, "null is used as a sentinal value and is not a valid item value for a hybrid collection");
            _singleItem = initialItem;
        }

        public T this[int index]
        {
            get
            {
                if (_singleItem != null)
                {
                    Fx.Assert(index == 0, "Out of range with a single item");
                    return _singleItem;
                }
                else if (_multipleItems != null)
                {
                    Fx.Assert(index >= 0 && index < _multipleItems.Count, "Out of range with multiple items.");

                    return _multipleItems[index];
                }

                Fx.Assert("Out of range.  There were no items in the HybridCollection.");
                return default(T);
            }
        }

        public int Count
        {
            get
            {
                if (_singleItem != null)
                {
                    return 1;
                }

                if (_multipleItems != null)
                {
                    return _multipleItems.Count;
                }

                return 0;
            }
        }

        protected T SingleItem
        {
            get
            {
                return _singleItem;
            }
        }

        protected IList<T> MultipleItems
        {
            get
            {
                return _multipleItems;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "multipleItems")]
        internal List<T> SerializedMultipleItems
        {
            get { return _multipleItems; }
            set { _multipleItems = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "singleItem")]
        internal T SerializedSingleItem
        {
            get { return _singleItem; }
            set { _singleItem = value; }
        }

        public void Add(T item)
        {
            Fx.Assert(item != null, "null is used as a sentinal value and is not a valid item value for a hybrid collection");
            if (_multipleItems != null)
            {
                _multipleItems.Add(item);
            }
            else if (_singleItem != null)
            {
                _multipleItems = new List<T>(2);
                _multipleItems.Add(_singleItem);
                _multipleItems.Add(item);
                _singleItem = null;
            }
            else
            {
                _singleItem = item;
            }
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            if (_multipleItems != null)
            {
                return new ReadOnlyCollection<T>(_multipleItems);
            }
            else if (_singleItem != null)
            {
                return new ReadOnlyCollection<T>(new T[1] { _singleItem });
            }
            else
            {
                return new ReadOnlyCollection<T>(new T[0]);
            }
        }

        // generally used for serialization purposes
        public void Compress()
        {
            if (_multipleItems != null && _multipleItems.Count == 1)
            {
                _singleItem = _multipleItems[0];
                _multipleItems = null;
            }
        }

        public void Remove(T item)
        {
            Remove(item, false);
        }

        internal void Remove(T item, bool searchingFromEnd)
        {
            if (_singleItem != null)
            {
                Fx.Assert(object.Equals(item, _singleItem), "The given item should be in this list. Something is wrong in our housekeeping.");
                _singleItem = null;
            }
            else
            {
                Fx.Assert(_multipleItems != null && _multipleItems.Contains(item), "The given item should be in this list. Something is wrong in our housekeeping.");
                int position = (searchingFromEnd) ? _multipleItems.LastIndexOf(item) : _multipleItems.IndexOf(item);
                if (position != -1)
                {
                    _multipleItems.RemoveAt(position);
                }
            }
        }
    }
}
