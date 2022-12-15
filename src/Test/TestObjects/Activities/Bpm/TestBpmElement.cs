// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Activities.Statements;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities.Tracing;

namespace Test.Common.TestObjects.Activities
{
    public abstract class TestBpmElement
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
        public static implicit operator TestBpmElement(TestActivity activity)
        {
            return new TestBpmStep { ActionActivity = activity };
        }

        internal abstract Outcome GetTrace(TraceGroup traceGroup);
        public abstract BpmNode GetProductElement();

        //This is needed to return the next element based on the hints (for conditional elements)
        internal abstract TestBpmElement GetNextElement();
    }
}
