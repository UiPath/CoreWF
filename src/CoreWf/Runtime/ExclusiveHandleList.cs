// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Runtime
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class ExclusiveHandleList : HybridCollection<ExclusiveHandle>
    {
        public ExclusiveHandleList()
            : base() { }

        internal bool Contains(ExclusiveHandle handle)
        {
            if (this.SingleItem != null)
            {
                if (this.SingleItem.Equals(handle))
                {
                    return true;
                }
            }
            else if (this.MultipleItems != null)
            {
                for (int i = 0; i < this.MultipleItems.Count; i++)
                {
                    if (handle.Equals(this.MultipleItems[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }
}


