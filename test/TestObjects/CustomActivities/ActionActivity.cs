// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;

namespace Test.Common.TestObjects.CustomActivities
{
    public class ActionActivity : Activity
    {
        public ActionActivity()
        {
            base.Implementation = () => new InvokeAction { Action = Action };
        }

        public ActivityAction Action { get; set; }

        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            // None
        }
    }
}
