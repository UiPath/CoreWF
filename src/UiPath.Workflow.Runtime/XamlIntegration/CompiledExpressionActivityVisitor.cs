// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;

namespace System.Activities.XamlIntegration;

internal abstract class CompiledExpressionActivityVisitor
{
    protected bool ForImplementation { get; private set; }

    public void Visit(Activity activity, bool forImplementation)
    {
        ForImplementation = forImplementation;
        VisitRoot(activity);
    }

    private void VisitCore(Activity activity)
    {
        if (activity is ITextExpression)
        {
            VisitITextExpression(activity);
            return;
        }
        // Look for variable scopes
        if (activity.RuntimeVariables != null && activity.RuntimeVariables.Count > 0)
        {
            VisitVariableScope(activity);
        }
        else
        {
            Visit(activity);
        }

        return;
    }

    protected virtual void Visit(Activity activity)
    {
        VisitArguments(activity);
        VisitPublicActivities(activity);
    }

    protected virtual void VisitRoot(Activity activity)
    {
        if (ForImplementation)
        {
            VisitRootImplementation(activity);
        }
        else
        {
            VisitRootPublic(activity);
        }
    }

    protected virtual void VisitRootImplementationArguments(Activity activity)
    {
        VisitArguments(activity, VisitRootImplementationArgument);
    }

    protected virtual void VisitRootImplementationArgument(RuntimeArgument runtimeArgument)
    {
        if (runtimeArgument.IsBound)
        {
            Activity expression = runtimeArgument.BoundArgument.Expression;
            if (expression != null)
            {
                VisitCore(expression);
            }
        }
    }

    protected virtual void VisitVariableScope(Activity activity)
    {
        //
        // Walk the contained variables' default expressions
        foreach (Variable v in activity.RuntimeVariables)
        {
            if (v.Default != null)
            {
                VisitCore(v.Default);
            }
        }

        VisitVariableScopeArguments(activity);

        VisitPublicActivities(activity);
    }

    protected virtual void VisitRootImplementationScope(Activity activity)
    {
        foreach (Variable v in activity.RuntimeVariables)
        {
            if (v.Default != null)
            {
                VisitCore(v.Default);
            }
        }

        VisitImportedChildren(activity);

        VisitImportedDelegates(activity);
    }

    protected virtual void VisitITextExpression(Activity activity) { }

    protected virtual void VisitChildren(Activity activity)
    {
        if (activity.Children != null)
        {
            for (int i = 0; i < activity.Children.Count; i++)
            {
                if (activity == activity.Children[i].Parent)
                {
                    VisitCore(activity.Children[i]);
                }
            }
        }
    }

    protected virtual void VisitImportedChildren(Activity activity)
    {
        if (activity.ImportedChildren != null)
        {
            for (int i = 0; i < activity.ImportedChildren.Count; i++)
            {
                VisitCore(activity.ImportedChildren[i]);
            }
        }
    }

    protected virtual void VisitDelegates(Activity activity)
    {
        if (activity.Delegates != null)
        {
            foreach (ActivityDelegate activityDelegate in activity.Delegates)
            {
                if (activity == activityDelegate.Owner)
                {
                    VisitDelegate(activityDelegate);
                }
            }
        }
    }

    protected virtual void VisitImportedDelegates(Activity activity)
    {
        if (activity.ImportedDelegates != null)
        {
            foreach (ActivityDelegate activityDelegate in activity.ImportedDelegates)
            {
                VisitDelegate(activityDelegate);
            }
        }
    }

    protected virtual void VisitDelegate(ActivityDelegate activityDelegate)
    {
        VisitDelegateArguments(activityDelegate);

        if (activityDelegate.Handler != null)
        {
            VisitCore(activityDelegate.Handler);
        }
    }

    protected virtual void VisitDelegateArguments(ActivityDelegate activityDelegate)
    {
        foreach (RuntimeDelegateArgument delegateArgument in activityDelegate.RuntimeDelegateArguments)
        {
            if (delegateArgument.BoundArgument != null)
            {
                VisitDelegateArgument(delegateArgument);
            }
        }
    }

    protected virtual void VisitDelegateArgument(RuntimeDelegateArgument delegateArgument) { }

    protected virtual void VisitVariableScopeArguments(Activity activity)
    {
        VisitArguments(activity, VisitVariableScopeArgument);
    }

    protected virtual void VisitVariableScopeArgument(RuntimeArgument runtimeArgument)
    {
        VisitArgument(runtimeArgument);
    }

    protected virtual void VisitArguments(Activity activity)
    {
        VisitArguments(activity, VisitArgument);
    }

    protected virtual void VisitArgument(RuntimeArgument runtimeArgument)
    {
        if (runtimeArgument.IsBound)
        {
            Activity expression = runtimeArgument.BoundArgument.Expression;
            if (expression != null)
            {
                VisitCore(expression);
            }
        }
    }

    private void VisitRootPublic(Activity activity)
    {
        if (activity.RuntimeVariables != null && activity.RuntimeVariables.Count > 0)
        {
            VisitVariableScope(activity);
        }
        else
        {
            VisitArguments(activity);

            VisitPublicActivities(activity);
        }
    }

    private void VisitRootImplementation(Activity activity)
    {
        VisitRootImplementationArguments(activity);

        VisitRootImplementationScope(activity);

        if (activity.ImplementationChildren != null)
        {
            for (int i = 0; i < activity.ImplementationChildren.Count; i++)
            {
                VisitCore(activity.ImplementationChildren[i]);
            }
        }
    }

    private void VisitPublicActivities(Activity activity)
    {
        VisitChildren(activity);
        {
            return;
        }

        VisitDelegates(activity);

        VisitImportedChildren(activity);

        VisitImportedDelegates(activity);
    }

    private static void VisitArguments(Activity activity, VisitArgumentDelegate visitArgument)
    {
        foreach (RuntimeArgument runtimeArgument in activity.RuntimeArguments)
        {
            visitArgument(runtimeArgument);
        }
    }

    private delegate void VisitArgumentDelegate(RuntimeArgument runtimeArgument);
}
