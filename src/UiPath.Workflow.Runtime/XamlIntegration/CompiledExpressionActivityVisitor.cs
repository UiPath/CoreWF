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
        VisitRoot(activity, out _);
    }

    private void VisitCore(Activity activity, out bool exit)
    {
        if (activity is ITextExpression)
        {
            VisitITextExpression(activity, out exit);
            return;
        }
        // Look for variable scopes
        if (activity.RuntimeVariables != null && activity.RuntimeVariables.Count > 0)
        {
            VisitVariableScope(activity, out exit);
            if (exit)
            {
                return;
            }
        }
        else
        {
            Visit(activity, out exit);
            if (exit)
            {
                return;
            }
        }

        return;
    }

    protected virtual void Visit(Activity activity, out bool exit)
    {
        VisitArguments(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitPublicActivities(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitImplementationChildren(activity, out exit);
        if (exit)
        {
            return;
        }
    }

    protected virtual void VisitRoot(Activity activity, out bool exit)
    {
        if (ForImplementation)
        {
            VisitRootImplementation(activity, out exit);
            if (exit)
            {
                return;
            }

            exit = false;
        }
        else
        {
            VisitRootPublic(activity, out exit);
            if (exit)
            {
                return;
            }

            exit = false;
        }
    }

    protected virtual void VisitRootImplementationArguments(Activity activity, out bool exit)
    {
        VisitArguments(activity, out exit, VisitRootImplementationArgument);
        if (exit)
        {
            return;
        }

        exit = false;
    }

    protected virtual void VisitRootImplementationArgument(RuntimeArgument runtimeArgument, out bool exit)
    {
        if (runtimeArgument.IsBound)
        {
            Activity expression = runtimeArgument.BoundArgument.Expression;
            if (expression != null)
            {
                VisitCore(expression, out exit);
                if (exit)
                {
                    return;
                }
            }
        }
        exit = false;
    }

    protected virtual void VisitVariableScope(Activity activity, out bool exit)
    {
        //
        // Walk the contained variables' default expressions
        foreach (Variable v in activity.RuntimeVariables)
        {
            if (v.Default != null)
            {
                VisitCore(v.Default, out exit);
                if (exit)
                {
                    return;
                }
            }
        }

        VisitVariableScopeArguments(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitPublicActivities(activity, out exit);
        if (exit)
        {
            return;
        }

        exit = false;
    }

    protected virtual void VisitRootImplementationScope(Activity activity, out bool exit)
    {
        foreach (Variable v in activity.RuntimeVariables)
        {
            if (v.Default != null)
            {
                VisitCore(v.Default, out exit);
                if (exit)
                {
                    return;
                }
            }
        }

        VisitImportedChildren(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitImportedDelegates(activity, out exit);
        if (exit)
        {
            return;
        }
    }

    protected virtual void VisitITextExpression(Activity activity, out bool exit) => exit = false;

    protected virtual void VisitChildren(Activity activity, out bool exit)
    {
        if (activity.Children != null)
        {
            for (int i = 0; i < activity.Children.Count; i++)
            {
                if (activity == activity.Children[i].Parent)
                {
                    VisitCore(activity.Children[i], out exit);
                    if (exit)
                    {
                        return;
                    }
                }
            }
        }
        exit = false;
    }

    protected virtual void VisitImportedChildren(Activity activity, out bool exit)
    {
        if (activity.ImportedChildren != null)
        {
            for (int i = 0; i < activity.ImportedChildren.Count; i++)
            {
                VisitCore(activity.ImportedChildren[i], out exit);
                if (exit)
                {
                    return;
                }
            }
        }
        exit = false;
    }

    protected virtual void VisitImplementationChildren(Activity activity, out bool exit)
    {
        if (activity.ImplementationChildren is not null)
        {
            for (int i = 0; i < activity.ImplementationChildren.Count; i++)
            {
                VisitCore(activity.ImplementationChildren[i], out exit);
                if (exit)
                {
                    return;
                }
            }
        }
        exit = false;
    }

    protected virtual void VisitDelegates(Activity activity, out bool exit)
    {
        if (activity.Delegates != null)
        {
            foreach (ActivityDelegate activityDelegate in activity.Delegates)
            {
                if (activity == activityDelegate.Owner)
                {
                    VisitDelegate(activityDelegate, out exit);

                    if (exit)
                    {
                        return;
                    }
                }
            }
        }
        exit = false;
    }

    protected virtual void VisitImportedDelegates(Activity activity, out bool exit)
    {
        if (activity.ImportedDelegates != null)
        {
            foreach (ActivityDelegate activityDelegate in activity.ImportedDelegates)
            {
                VisitDelegate(activityDelegate, out exit);

                if (exit)
                {
                    return;
                }
            }
        }
        exit = false;
    }

    protected virtual void VisitDelegate(ActivityDelegate activityDelegate, out bool exit)
    {
        VisitDelegateArguments(activityDelegate, out exit);
        if (exit)
        {
            return;
        }

        if (activityDelegate.Handler != null)
        {
            VisitCore(activityDelegate.Handler, out exit);
            if (exit)
            {
                return;
            }
        }
    }

    protected virtual void VisitDelegateArguments(ActivityDelegate activityDelegate, out bool exit)
    {
        foreach (RuntimeDelegateArgument delegateArgument in activityDelegate.RuntimeDelegateArguments)
        {
            if (delegateArgument.BoundArgument != null)
            {
                VisitDelegateArgument(delegateArgument, out exit);

                if (exit)
                {
                    return;
                }
            }
        }

        exit = false;
    }

    protected virtual void VisitDelegateArgument(RuntimeDelegateArgument delegateArgument, out bool exit) => exit = false;

    protected virtual void VisitVariableScopeArguments(Activity activity, out bool exit)
    {
        VisitArguments(activity, out exit, VisitVariableScopeArgument);
        if (exit)
        {
            return;
        }

        exit = false;
    }

    protected virtual void VisitVariableScopeArgument(RuntimeArgument runtimeArgument, out bool exit)
    {
        VisitArgument(runtimeArgument, out exit);
        if (exit)
        {
            return;
        }

        exit = false;
    }

    protected virtual void VisitArguments(Activity activity, out bool exit)
    {
        VisitArguments(activity, out exit, VisitArgument);
        if (exit)
        {
            return;
        }

        exit = false;
    }

    protected virtual void VisitArgument(RuntimeArgument runtimeArgument, out bool exit)
    {
        if (runtimeArgument.IsBound)
        {
            Activity expression = runtimeArgument.BoundArgument.Expression;
            if (expression != null)
            {
                VisitCore(expression, out exit);
                if (exit)
                {
                    return;
                }
            }
        }
        exit = false;
    }

    private void VisitRootPublic(Activity activity, out bool exit)
    {
        if (activity.RuntimeVariables != null && activity.RuntimeVariables.Count > 0)
        {
            VisitVariableScope(activity, out exit);
            if (exit)
            {
                return;
            }
        }
        else
        {
            VisitArguments(activity, out exit);
            if (exit)
            {
                return;
            }

            VisitPublicActivities(activity, out exit);
            if (exit)
            {
                return;
            }
        }
    }

    private void VisitRootImplementation(Activity activity, out bool exit)
    {
        VisitRootImplementationArguments(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitRootImplementationScope(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitImplementationChildren(activity, out exit);
        if (exit)
        {
            return;
        }
    }

    private void VisitPublicActivities(Activity activity, out bool exit)
    {
        VisitChildren(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitDelegates(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitImportedChildren(activity, out exit);
        if (exit)
        {
            return;
        }

        VisitImportedDelegates(activity, out exit);
        if (exit)
        {
            return;
        }
    }

    private static void VisitArguments(Activity activity, out bool exit, VisitArgumentDelegate visitArgument)
    {
        foreach (RuntimeArgument runtimeArgument in activity.RuntimeArguments)
        {
            visitArgument(runtimeArgument, out exit);
            if (exit)
            {
                return;
            }
        }
        exit = false;
    }

    private delegate void VisitArgumentDelegate(RuntimeArgument runtimeArgument, out bool exit);
}
