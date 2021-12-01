// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public class WorkflowApplicationCompletedEventArgs : WorkflowApplicationEventArgs
{
    private readonly ActivityInstanceState _completionState;
    private readonly Exception _terminationException;
    private IDictionary<string, object> _outputs;

    internal WorkflowApplicationCompletedEventArgs(WorkflowApplication application, Exception terminationException, ActivityInstanceState completionState, IDictionary<string, object> outputs)
        : base(application)
    {
        Fx.Assert(ActivityUtilities.IsCompletedState(completionState), "event should only fire for completed activities");
        _terminationException = terminationException;
        _completionState = completionState;
        _outputs = outputs;
    }

    public ActivityInstanceState CompletionState => _completionState;

    public IDictionary<string, object> Outputs
    {
        get
        {
            _outputs ??= ActivityUtilities.EmptyParameters;
            return _outputs;
        }
    }

    public Exception TerminationException => _terminationException;
}
