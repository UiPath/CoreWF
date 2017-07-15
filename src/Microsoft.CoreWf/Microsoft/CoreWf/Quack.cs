// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf.Runtime;

namespace CoreWf
{
    // A mostly output-restricted double-ended queue. You can add an item to both ends
    // and it is optimized for removing from the front.  The list can be scanned and
    // items can be removed from any location at the cost of performance.
    internal class Quack<T>
    {
        private T[] _items;

        // First element when items is not empty
        private int _head;
        // Next vacancy when items are not full
        private int _tail;
        // Number of elements.
        private int _count;

        public Quack()
        {
            _items = new T[4];
        }

        public Quack(T[] items)
        {
            Fx.Assert(items != null, "This shouldn't get called with null");
            Fx.Assert(items.Length > 0, "This shouldn't be called with a zero length array.");

            _items = items;

            // The default value of 0 is correct for both
            // head and tail.

            _count = _items.Length;
        }

        public int Count
        {
            get { return _count; }
        }

        public T this[int index]
        {
            get
            {
                Fx.Assert(index < _count, "Index out of range.");

                int realIndex = (_head + index) % _items.Length;

                return _items[realIndex];
            }
        }

        public T[] ToArray()
        {
            Fx.Assert(_count > 0, "We should only call this when we have items.");

            T[] compressedItems = new T[_count];

            for (int i = 0; i < _count; i++)
            {
                compressedItems[i] = _items[(_head + i) % _items.Length];
            }

            return compressedItems;
        }

        public void PushFront(T item)
        {
            if (_count == _items.Length)
            {
                Enlarge();
            }

            if (--_head == -1)
            {
                _head = _items.Length - 1;
            }
            _items[_head] = item;

            ++_count;
        }

        public void Enqueue(T item)
        {
            if (_count == _items.Length)
            {
                Enlarge();
            }

            _items[_tail] = item;
            if (++_tail == _items.Length)
            {
                _tail = 0;
            }

            ++_count;
        }

        public T Dequeue()
        {
            Fx.Assert(_count > 0, "Quack is empty");

            T removed = _items[_head];
            _items[_head] = default(T);
            if (++_head == _items.Length)
            {
                _head = 0;
            }

            --_count;

            return removed;
        }

        public bool Remove(T item)
        {
            int found = -1;

            for (int i = 0; i < _count; i++)
            {
                int realIndex = (_head + i) % _items.Length;
                if (object.Equals(_items[realIndex], item))
                {
                    found = i;
                    break;
                }
            }

            if (found == -1)
            {
                return false;
            }
            else
            {
                Remove(found);
                return true;
            }
        }

        public void Remove(int index)
        {
            Fx.Assert(index < _count, "Index out of range");

            for (int i = index - 1; i >= 0; i--)
            {
                int sourceIndex = (_head + i) % _items.Length;
                int targetIndex = sourceIndex + 1;

                if (targetIndex == _items.Length)
                {
                    targetIndex = 0;
                }

                _items[targetIndex] = _items[sourceIndex];
            }

            --_count;
            ++_head;

            if (_head == _items.Length)
            {
                _head = 0;
            }
        }

        private void Enlarge()
        {
            Fx.Assert(_items.Length > 0, "Quack is empty");

            int capacity = _items.Length * 2;
            this.SetCapacity(capacity);
        }

        private void SetCapacity(int capacity)
        {
            Fx.Assert(capacity >= _count, "Capacity is set to a smaller value");

            T[] newArray = new T[capacity];
            if (_count > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_items, _head, newArray, 0, _count);
                }
                else
                {
                    Array.Copy(_items, _head, newArray, 0, _items.Length - _head);
                    Array.Copy(_items, 0, newArray, _items.Length - _head, _tail);
                }
            }

            _items = newArray;
            _head = 0;
            _tail = (_count == capacity) ? 0 : _count;
        }
    }
}
