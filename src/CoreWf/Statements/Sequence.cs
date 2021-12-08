// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Activities")]
public sealed class Sequence : NativeActivity
{
    private Collection<Activity> _activities;
    private Collection<Variable> _variables;
    private readonly Variable<int> _lastIndexHint;
    private readonly CompletionCallback _onChildComplete;

    public Sequence()
        : base()
    {
        _lastIndexHint = new Variable<int>();
        _onChildComplete = new CompletionCallback(InternalExecute);
    }

    public Collection<Variable> Variables
    {
        get
        {
            _variables ??= new ValidatingCollection<Variable>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _variables;
        }
    }

    [DependsOn("Variables")]
    public Collection<Activity> Activities
    {
        get
        {
            _activities ??= new ValidatingCollection<Activity>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _activities;
        }
    }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        // Our algorithm for recovering from update depends on iterating a unique Activities list.
        // So we can't support update if the same activity is referenced more than once.
        for (int i = 0; i < this.Activities.Count - 1; i++)
        {
            for (int j = i + 1; j < this.Activities.Count; j++)
            {
                if (this.Activities[i] == this.Activities[j])
                {
                    metadata.DisallowUpdateInsideThisActivity(SR.SequenceDuplicateReferences);
                    break;
                }
            }
        }
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.SetChildrenCollection(Activities);
        metadata.SetVariablesCollection(Variables);
        metadata.AddImplementationVariable(_lastIndexHint);
    }

    protected override void Execute(NativeActivityContext context)
    {
        if (_activities != null && Activities.Count > 0)
        {
            Activity nextChild = Activities[0];

            context.ScheduleActivity(nextChild, _onChildComplete);
        }
    }

    private void InternalExecute(NativeActivityContext context, ActivityInstance completedInstance)
    {
        int completedInstanceIndex = _lastIndexHint.Get(context);

        if (completedInstanceIndex >= Activities.Count || Activities[completedInstanceIndex] != completedInstance.Activity)
        {
            completedInstanceIndex = Activities.IndexOf(completedInstance.Activity);
        }

        int nextChildIndex = completedInstanceIndex + 1;

        if (nextChildIndex == Activities.Count)
        {
            return;
        }

        Activity nextChild = Activities[nextChildIndex];

        context.ScheduleActivity(nextChild, _onChildComplete);

        _lastIndexHint.Set(context, nextChildIndex);
    }
}
