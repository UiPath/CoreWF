// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using System.Collections.Generic;

namespace Test.Common.TestObjects.CustomActivities
{
    public class ResultScope<TResult> : NativeActivity<TResult>
    {
        private Activity _body;
        private Variable<TResult> _resultVariable;

        public ResultScope()
            : base()
        {
        }

        public Variable<TResult> ResultVariable
        {
            get { return _resultVariable; }
            set
            {
                _resultVariable = value;
            }
        }

        public Activity Body
        {
            get { return _body; }
            set
            {
                _body = value;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddChild(this.Body);
            metadata.AddVariable(this.ResultVariable);
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.ScheduleActivity(this.Body, new CompletionCallback(OnBodyComplete));
        }

        private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance)
        {
            context.SetValue(this.Result, this.ResultVariable.Get(context));
        }
    }
}
