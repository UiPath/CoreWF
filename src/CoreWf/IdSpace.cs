// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;

internal class IdSpace
{
    private int _lastId;
    private IList<Activity> _members;

    public IdSpace() { }

    public IdSpace(IdSpace parent, int parentId)
    {
        Parent = parent;
        ParentId = parentId;
    }

    public IdSpace Parent { get; private set; }

    public int ParentId { get; private set; }

    public int MemberCount => _members == null ? 0 : _members.Count;

    public Activity Owner => Parent?[ParentId];

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
        _members ??= new List<Activity>();

        if (_lastId == int.MaxValue)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.OutOfIdSpaceIds));
        }

        _lastId++;
            
        // ID info is cleared inside InternalId.
        element.InternalId = _lastId;
        Fx.Assert(element.MemberOf == this, "We should have already set ");
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
        Parent = null;
        ParentId = 0;
    }
}
