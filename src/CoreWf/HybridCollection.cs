// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities;
using Runtime;

// used internally for performance in cases where a common usage pattern is a single item
[DataContract]
internal class HybridCollection<T>
    where T : class
{
    private List<T> _multipleItems;
    private T _singleItem;

    public HybridCollection() { }

    public HybridCollection(T initialItem)
    {
        Fx.Assert(initialItem != null, "null is used as a sentinel value and is not a valid item value for a hybrid collection");
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
            return default;
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

    protected T SingleItem => _singleItem;

    protected IList<T> MultipleItems => _multipleItems;

    [DataMember(EmitDefaultValue = false, Name = "multipleItems")]
    internal List<T> SerializedMultipleItems
    {
        get => _multipleItems;
        set => _multipleItems = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "singleItem")]
    internal T SerializedSingleItem
    {
        get => _singleItem;
        set => _singleItem = value;
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
            _multipleItems = new List<T>(2)
            {
                _singleItem,
                item
            };
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
            return new ReadOnlyCollection<T>(Array.Empty<T>());
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

    public void Remove(T item) => Remove(item, false);

    internal void Remove(T item, bool searchingFromEnd)
    {
        if (_singleItem != null)
        {
            Fx.Assert(Equals(item, _singleItem), "The given item should be in this list. Something is wrong in our housekeeping.");
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
