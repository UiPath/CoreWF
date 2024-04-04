// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace LegacyTest.Test.Common.TestObjects.Activities.Collections
{
    public class MemberCollection<T> : ICollection<T>
    {
        private readonly AddItemDelegate _addItem;
        private List<T> _memberList = new List<T>();

        private InsertItemDelegate _insertItem;
        private RemoveItemDelegate _removeItem;
        private RemoveAtItemDelegate _removeAtItem;

        public MemberCollection(AddItemDelegate addItem)
        {
            _addItem = addItem;
        }

        public InsertItemDelegate InsertItem
        {
            set { _insertItem = value; }
        }

        public RemoveAtItemDelegate RemoveAtItem
        {
            set { _removeAtItem = value; }
        }

        public RemoveItemDelegate RemoveItem
        {
            set { _removeItem = value; }
        }

        #region ICollection<T> Members

        public int Count
        {
            get { return _memberList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public T this[int i]
        {
            get
            {
                return (T)_memberList[i];
            }
        }

        public void Add(T item)
        {
            _memberList.Add(item);
            _addItem(item);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            return _memberList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            if (null != _removeItem)
            {
                if (_removeItem(item))
                {
                    return _memberList.Remove(item);
                }
                else
                {
                    throw new Exception("Remove failed");
                }
            }
            else
            {
                throw new NotImplementedException("Remove from MemberCollection failed. RemoveItemDelegate is null.");
            }
        }

        public void RemoveAt(int index)
        {
            if (null != _removeAtItem)
            {
                _removeAtItem(index);
                _memberList.RemoveAt(index);
            }
            else
            {
                throw new NotImplementedException("RemoveAt from MemberCollection failed. RemoveAtItemDelegate is null.");
            }
        }

        public void Insert(int index, T item)
        {
            if (null != _insertItem)
            {
                _insertItem(index, item);
                _memberList.Insert(index, item);
            }
            else
            {
                throw new NotImplementedException("Insert in MemberCollection failed. InsertItemDelegate is null.");
            }
        }
        #endregion

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_memberList).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _memberList.GetEnumerator();
        }

        public delegate void AddItemDelegate(T item);

        public delegate void InsertItemDelegate(int index, T item);

        public delegate bool RemoveItemDelegate(T item);

        public delegate void RemoveAtItemDelegate(int index);
    }
}
