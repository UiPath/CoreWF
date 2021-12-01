// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
internal class ExclusiveHandleList : HybridCollection<ExclusiveHandle>
{
    public ExclusiveHandleList()
        : base() { }

    internal bool Contains(ExclusiveHandle handle)
    {
        if (SingleItem != null)
        {
            if (SingleItem.Equals(handle))
            {
                return true;
            }
        }
        else if (MultipleItems != null)
        {
            for (int i = 0; i < MultipleItems.Count; i++)
            {
                if (handle.Equals(MultipleItems[i]))
                {
                    return true;
                }
            }
        }

        return false;
    }

}
