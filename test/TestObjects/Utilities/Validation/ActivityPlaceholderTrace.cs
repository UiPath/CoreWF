// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Test.Common.TestObjects.Activities;

namespace Test.Common.TestObjects.Utilities.Validation
{
    public class ActivityPlaceholderTrace : PlaceholderTrace
    {
        private TestActivity _body;

        public ActivityPlaceholderTrace(TestActivity body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }

            _body = body;
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
