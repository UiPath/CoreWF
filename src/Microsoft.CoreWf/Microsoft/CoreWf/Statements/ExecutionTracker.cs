// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Statements
{
    [DataContract]
    internal sealed class ExecutionTracker
    {
        private List<CompensationTokenData> _executionOrderedList;

        public ExecutionTracker()
        {
            _executionOrderedList = new List<CompensationTokenData>();
        }

        public int Count
        {
            get
            {
                return _executionOrderedList.Count;
            }
        }

        [DataMember(Name = "executionOrderedList")]
        internal List<CompensationTokenData> SerializedExecutionOrderedList
        {
            get { return _executionOrderedList; }
            set { _executionOrderedList = value; }
        }

        public void Add(CompensationTokenData compensationToken)
        {
            _executionOrderedList.Insert(0, compensationToken);
        }

        public void Remove(CompensationTokenData compensationToken)
        {
            _executionOrderedList.Remove(compensationToken);
        }

        public CompensationTokenData Get()
        {
            if (Count > 0)
            {
                return _executionOrderedList[0];
            }
            else
            {
                return null;
            }
        }
    }
}
