// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace System.Activities.Statements;

[ContentProperty("Cases")]
public sealed class Switch<T> : NativeActivity
{
    private IDictionary<T, Activity> _cases;

    public Switch() { }

    public Switch(Expression<Func<ActivityContext, T>> expression)
        : this()
    {
        if (expression == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(expression));
        }

        Expression = new InArgument<T>(expression);
    }

    public Switch(Activity<T> expression)
        : this()
    {
        if (expression == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(expression));
        }

        Expression = new InArgument<T>(expression);
    }

    public Switch(InArgument<T> expression)
        : this()
    {
        Expression = expression ?? throw FxTrace.Exception.ArgumentNull(nameof(expression));
    }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<T> Expression { get; set; }

    public IDictionary<T, Activity> Cases
    {
        get
        {
            _cases ??= new NullableKeyDictionary<T, Activity>();
            return _cases;
        }
    }

    [DefaultValue(null)]
    public Activity Default { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(DynamicUpdate.NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void CacheMetadata(NativeActivityMetadata metadata)
    {
        RuntimeArgument expressionArgument = new("Expression", typeof(T), ArgumentDirection.In, true);
        metadata.Bind(Expression, expressionArgument);
        metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { expressionArgument });

        Collection<Activity> children = new();

        foreach (Activity child in Cases.Values)
        {
            children.Add(child);
        }

        if (Default != null)
        {
            children.Add(Default);
        }

        metadata.SetChildrenCollection(children);
    }

    protected override void Execute(NativeActivityContext context)
    {
        T result = Expression.Get(context);

        if (!Cases.TryGetValue(result, out Activity selection))
        {
            if (Default != null)
            {
                selection = Default;
            }
            else
            {
                if (TD.SwitchCaseNotFoundIsEnabled())
                {
                    TD.SwitchCaseNotFound(DisplayName);
                }
            }
        }

        if (selection != null)
        {
            context.ScheduleActivity(selection);
        }
    }
}
