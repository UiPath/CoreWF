// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.Generic;

namespace Microsoft.CoreWf
{
    [Fx.Tag.XamlVisible(false)]
    public class WorkflowApplicationCompletedEventArgs : WorkflowApplicationEventArgs
    {
        private ActivityInstanceState _completionState;
        private Exception _terminationException;
        private IDictionary<string, object> _outputs;

        internal WorkflowApplicationCompletedEventArgs(WorkflowApplication application, Exception terminationException, ActivityInstanceState completionState, IDictionary<string, object> outputs)
            : base(application)
        {
            Fx.Assert(ActivityUtilities.IsCompletedState(completionState), "event should only fire for completed activities");
            _terminationException = terminationException;
            _completionState = completionState;
            _outputs = outputs;
        }

        public ActivityInstanceState CompletionState
        {
            get
            {
                return _completionState;
            }
        }

        public IDictionary<string, object> Outputs
        {
            get
            {
                if (_outputs == null)
                {
                    _outputs = ActivityUtilities.EmptyParameters;
                }
                return _outputs;
            }
        }

        public Exception TerminationException
        {
            get
            {
                return _terminationException;
            }
        }
    }
}
