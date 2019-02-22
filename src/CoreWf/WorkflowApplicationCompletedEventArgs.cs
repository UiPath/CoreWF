// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Runtime;
    using System;
    using System.Collections.Generic;

    [Fx.Tag.XamlVisible(false)]
    public class WorkflowApplicationCompletedEventArgs : WorkflowApplicationEventArgs
    {
        private readonly ActivityInstanceState completionState;
        private readonly Exception terminationException;
        private IDictionary<string, object> outputs;

        internal WorkflowApplicationCompletedEventArgs(WorkflowApplication application, Exception terminationException, ActivityInstanceState completionState, IDictionary<string, object> outputs)
            : base(application)
        {
            Fx.Assert(ActivityUtilities.IsCompletedState(completionState), "event should only fire for completed activities");
            this.terminationException = terminationException;
            this.completionState = completionState;
            this.outputs = outputs;
        }

        public ActivityInstanceState CompletionState
        {
            get
            {
                return this.completionState;
            }
        }

        public IDictionary<string, object> Outputs
        {
            get
            {
                if (this.outputs == null)               
                {
                    this.outputs = ActivityUtilities.EmptyParameters;
                }
                return this.outputs;
            }
        }

        public Exception TerminationException
        {
            get
            {
                return this.terminationException;
            }
        }
    }
}
