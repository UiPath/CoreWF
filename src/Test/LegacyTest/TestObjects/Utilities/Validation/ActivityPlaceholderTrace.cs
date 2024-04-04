// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using LegacyTest.Test.Common.TestObjects.Activities;

namespace LegacyTest.Test.Common.TestObjects.Utilities.Validation
{
    public class ActivityPlaceholderTrace : PlaceholderTrace
    {
        private TestActivity _body;

        public ActivityPlaceholderTrace(TestActivity body)
        {
            _body = body ?? throw new ArgumentNullException("body");
            this.Provider = this;
        }

        public override TraceGroup GetPlaceholderTrace()
        {
            OrderedTraces trace = new OrderedTraces();

            _body.GetTrace(trace);
            return trace;
        }
    }
}
