// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using System;
using System.Collections.Generic;

namespace CoreWf
{
    internal class IdSpace
    {
        private int _lastId;
        private IList<Activity> _members;

        public IdSpace()
        {
        }

        public IdSpace(IdSpace parent, int parentId)
        {
            this.Parent = parent;
            this.ParentId = parentId;
        }

        public IdSpace Parent
        {
            get;
            private set;
        }

        public int ParentId
        {
            get;
            private set;
        }

        public int MemberCount
        {
            get
            {
                if (_members == null)
                {
                    return 0;
                }
                else
                {
                    return _members.Count;
                }
            }
        }

        public Activity Owner
        {
            get
            {
                if (this.Parent != null)
                {
                    return this.Parent[this.ParentId];
                }

                return null;
            }
        }

        public Activity this[int id]
        {
            get
            {
                int lookupId = id - 1;
                if (_members == null || lookupId < 0 || lookupId >= _members.Count)
                {
                    return null;
                }
                else
                {
                    return _members[lookupId];
                }
            }
        }

        public void AddMember(Activity element)
        {
            if (_members == null)
            {
                _members = new List<Activity>();
            }

            if (_lastId == int.MaxValue)
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(new NotSupportedException(SR.OutOfIdSpaceIds));
            }

            _lastId++;

            // ID info is cleared inside InternalId.
            element.InternalId = _lastId;
            Fx.Assert(element.MemberOf == this, "We should have already set this.");
            Fx.Assert(_members.Count == element.InternalId - 1, "We should always be adding the next element");

            _members.Add(element);
        }

        public void Dispose()
        {
            if (_members != null)
            {
                _members.Clear();
            }

            _lastId = 0;
            this.Parent = null;
            this.ParentId = 0;
        }
    }
}


