// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace CoreWf.Runtime
{
    [DataContract]
    internal class ExclusiveHandleList : HybridCollection<ExclusiveHandle>
    {
        public ExclusiveHandleList()
            : base()
        { }

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


