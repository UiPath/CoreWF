// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace System.Activities.Statements;

//[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords, Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [While])")]
[ContentProperty("Body")]
public sealed class While : NativeActivity
{
    private CompletionCallback _onBodyComplete;
    private CompletionCallback<bool> _onConditionComplete;
    private Collection<Variable> _variables;

    public While()
        : base() { }

    public While(Expression<Func<ActivityContext, bool>> condition)
        : this()
    {
        if (condition == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(condition));
        }

        Condition = new LambdaValue<bool>(condition);
    }

    public While(Activity<bool> condition)
        : this()
    {
        Condition = condition ?? throw FxTrace.Exception.ArgumentNull(nameof(condition));
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

    [DefaultValue(null)]
    [DependsOn("Variables")]
    public Activity<bool> Condition { get; set; }

    [DefaultValue(null)]
    [DependsOn("Condition")]
    public Activity Body { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    }
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        metadata.SetVariablesCollection(Variables);

        metadata.AddChild(Body);

        if (Condition == null)
        {
            metadata.AddValidationError(SR.WhileRequiresCondition(DisplayName));
        }
        else
        {
            metadata.AddChild(Condition);
        }
    }

    protected override void Execute(NativeActivityContext context) => ScheduleCondition(context);

    private void ScheduleCondition(NativeActivityContext context)
    {
        Fx.Assert(Condition != null, "validated in OnOpen");
        _onConditionComplete ??= new CompletionCallback<bool>(OnConditionComplete);
        context.ScheduleActivity(Condition, _onConditionComplete);
    }

    private void OnConditionComplete(NativeActivityContext context, ActivityInstance completedInstance, bool result)
    {
        if (result)
        {
            if (Body != null)
            {
                _onBodyComplete ??= new CompletionCallback(OnBodyComplete);
                context.ScheduleActivity(Body, _onBodyComplete);
            }
            else
            {
                ScheduleCondition(context);
            }
        }
    }

    private void OnBodyComplete(NativeActivityContext context, ActivityInstance completedInstance) => ScheduleCondition(context);
}
