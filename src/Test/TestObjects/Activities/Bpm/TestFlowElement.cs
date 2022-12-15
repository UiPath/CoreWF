// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Activities.Statements;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities.Bpm
{
    public abstract class TestFlowElement
    {
        [DefaultValue(false)]
        public virtual bool IsFaulting
        {
            get;
            set;
        }

        [DefaultValue(false)]
        public virtual bool IsCancelling
        {
            get;
            set;
        }
        public static implicit operator TestFlowElement(TestActivity activity)
        {
            return new TestFlowStep { ActionActivity = activity };
        }

        internal abstract Outcome GetTrace(TraceGroup traceGroup);
        public abstract FlowNode GetProductElement();

        //This is needed to return the next element based on the hints (for conditional elements)
        internal abstract TestFlowElement GetNextElement();
    }
}
