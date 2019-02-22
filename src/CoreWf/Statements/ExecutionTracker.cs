// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    internal sealed class ExecutionTracker
    {
        private List<CompensationTokenData> executionOrderedList;

        public ExecutionTracker()
        {
            this.executionOrderedList = new List<CompensationTokenData>();
        }

        public int Count
        {
            get
            {
                return this.executionOrderedList.Count;
            }
        }

        [DataMember(Name = "executionOrderedList")]
        internal List<CompensationTokenData> SerializedExecutionOrderedList
        {
            get { return this.executionOrderedList; }
            set { this.executionOrderedList = value; }
        }

        public void Add(CompensationTokenData compensationToken)
        {
            this.executionOrderedList.Insert(0, compensationToken);
        }

        public void Remove(CompensationTokenData compensationToken)
        {
            this.executionOrderedList.Remove(compensationToken);
        }

        public CompensationTokenData Get()
        {
            if (Count > 0)
            {
                return this.executionOrderedList[0];
            }
            else
            {
                return null;
            }
        }
    }
}
