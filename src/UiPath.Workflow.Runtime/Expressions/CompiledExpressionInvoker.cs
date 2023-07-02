// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.XamlIntegration;
using System.Linq.Expressions;
using System.Xaml;

namespace System.Activities.Expressions;

public class CompiledExpressionInvoker
{
    private static readonly AttachableMemberIdentifier compiledExpressionRootProperty =
        new(typeof(CompiledExpressionInvoker), "CompiledExpressionRoot");
    private static readonly AttachableMemberIdentifier compiledExpressionRootForImplementationProperty =
        new(typeof(CompiledExpressionInvoker), "CompiledExpressionRootForImplementation");
    private int _expressionId;
    private readonly ActivityWithResult _expressionActivity;
    private readonly bool _isReference;
    private readonly ITextExpression _textExpression;
    private readonly Activity _metadataRoot;
    private ICompiledExpressionRoot _compiledRoot;
    private readonly IList<LocationReference> _locationReferences;
    private CodeActivityMetadata _metadata;
    private CodeActivityPublicEnvironmentAccessor _accessor;

    public CompiledExpressionInvoker(ITextExpression expression, bool isReference, CodeActivityMetadata metadata)
    {
        _expressionId = -1;
        _textExpression = expression ?? throw FxTrace.Exception.ArgumentNull(nameof(expression));
        _expressionActivity = expression as ActivityWithResult;
        _isReference = isReference;
        _locationReferences = new List<LocationReference>();
        _metadata = metadata;
        _accessor = CodeActivityPublicEnvironmentAccessor.Create(_metadata);

        if (_expressionActivity == null)
        {
            throw FxTrace.Exception.Argument(nameof(expression), SR.ITextExpressionParameterMustBeActivity);
        }

        _metadataRoot = metadata.Environment.Root;

        ProcessLocationReferences();
    }

    public object InvokeExpression(ActivityContext activityContext)
    {
        if (activityContext == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(activityContext));
        }

        if (_compiledRoot == null || _expressionId < 0)
        {
            if (!TryGetCompiledExpressionRoot(_expressionActivity, _metadataRoot, out _compiledRoot) ||
                !CanExecuteExpression(_compiledRoot, out _expressionId))
            {
                if (!TryGetCurrentCompiledExpressionRoot(activityContext, out _compiledRoot, out _expressionId))
                {
                    throw FxTrace.Exception.AsError(new NotSupportedException(SR.TextExpressionMetadataRequiresCompilation(_expressionActivity.GetType().Name)));
                }
            }
        }

        return _compiledRoot.InvokeExpression(_expressionId, _locationReferences, activityContext);
    }
    //
    // Attached property setter for the compiled expression root for the public surface area of an activity
    public static void SetCompiledExpressionRoot(object target, ICompiledExpressionRoot compiledExpressionRoot)
    {
        if (compiledExpressionRoot == null)
        {
            AttachablePropertyServices.RemoveProperty(target, compiledExpressionRootProperty);
        }
        else
        {
            AttachablePropertyServices.SetProperty(target, compiledExpressionRootProperty, compiledExpressionRoot);
        }
    }

    //
    // Attached property getter for the compiled expression root for the public surface area of an activity
    public static object GetCompiledExpressionRoot(object target)
    {
        AttachablePropertyServices.TryGetProperty(target, compiledExpressionRootProperty, out object value);
        return value;
    }

    //
    // Attached property setter for the compiled expression root for the implementation surface area of an activity
    public static void SetCompiledExpressionRootForImplementation(object target, ICompiledExpressionRoot compiledExpressionRoot)
    {
        if (compiledExpressionRoot == null)
        {
            AttachablePropertyServices.RemoveProperty(target, compiledExpressionRootForImplementationProperty);
        }
        else
        {
            AttachablePropertyServices.SetProperty(target, compiledExpressionRootForImplementationProperty, compiledExpressionRoot);
        }
    }

    //
    // Attached property getter for the compiled expression root for the implementation surface area of an activity
    public static object GetCompiledExpressionRootForImplementation(object target)
    {
        AttachablePropertyServices.TryGetProperty(target, compiledExpressionRootForImplementationProperty, out object value);
        return value;
    }

    //
    // Internal helper to find the correct ICER for a given expression.
    internal static bool TryGetCompiledExpressionRoot(Activity expression, Activity target, out ICompiledExpressionRoot compiledExpressionRoot)
    {
        bool forImplementation = expression.MemberOf != expression.RootActivity.MemberOf;

        return TryGetCompiledExpressionRoot(target, forImplementation, out compiledExpressionRoot);
    }

    //
    // Helper to find the correct ICER for a given expression.
    // This is separate from the above because within this class we switch forImplementation for the same target Activity
    // to matched the ICER model of using one ICER for all expressions in the implementation and root argument defaults.
    internal static bool TryGetCompiledExpressionRoot(Activity target, bool forImplementation, out ICompiledExpressionRoot compiledExpressionRoot)
    {
        if (!forImplementation)
        {
            compiledExpressionRoot = GetCompiledExpressionRoot(target) as ICompiledExpressionRoot;
            if (compiledExpressionRoot != null)
            {
                return true;
            }
            //
            // Default expressions for Arguments show up in the public surface area
            // If we didn't find an ICER for the public surface area continue
            // and try to use the implementation ICER
        }

        if (target is ICompiledExpressionRoot root)
        {
            compiledExpressionRoot = root;
            return true;
        }

        compiledExpressionRoot = GetCompiledExpressionRootForImplementation(target) as ICompiledExpressionRoot;
        if (compiledExpressionRoot != null)
        {
            return true;
        }

        compiledExpressionRoot = null;
        return false;
    }

    internal Expression GetExpressionTree()
    {
        if (_compiledRoot == null || _expressionId < 0)
        {
            if (!TryGetCompiledExpressionRootAtDesignTime(_expressionActivity, _metadataRoot, out _compiledRoot, out _expressionId))
            {
                return null;
            }
        }

        return _compiledRoot.GetExpressionTreeForExpression(_expressionId, _locationReferences);
    }

    private bool TryGetCurrentCompiledExpressionRoot(ActivityContext activityContext, out ICompiledExpressionRoot compiledExpressionRoot, out int expressionId)
    {
        ActivityInstance current = activityContext.CurrentInstance;

        while (current != null && current.Activity != _metadataRoot)
        {

            if (TryGetCompiledExpressionRoot(current.Activity, true, out ICompiledExpressionRoot currentCompiledExpressionRoot))
            {
                if (CanExecuteExpression(currentCompiledExpressionRoot, out expressionId))
                {
                    compiledExpressionRoot = currentCompiledExpressionRoot;
                    return true;
                }
            }
            current = current.Parent;
        }

        compiledExpressionRoot = null;
        expressionId = -1;

        return false;
    }

    private bool CanExecuteExpression(ICompiledExpressionRoot compiledExpressionRoot, out int expressionId)
    {
        var resultType = _expressionActivity.ResultType;
        return compiledExpressionRoot.CanExecuteExpression(_isReference ? resultType.GenericTypeArguments[0] : resultType,
            _textExpression.ExpressionText, _isReference, _locationReferences, out expressionId); ;
    }

    private void ProcessLocationReferences()
    {
        Stack<LocationReferenceEnvironment> environments = new();
        //
        // Build list of location by enumerating environments
        // in top down order to match the traversal pattern of TextExpressionCompiler
        LocationReferenceEnvironment current = _accessor.ActivityMetadata.Environment;
        while (current != null)
        {
            environments.Push(current);
            current = current.Parent;
        }

        bool requiresCompilation = _textExpression.Language != "VB";

        foreach (LocationReferenceEnvironment environment in environments)
        {
            foreach (LocationReference reference in environment.GetLocationReferences())
            {
                if (requiresCompilation)
                {
                    _accessor.CreateLocationArgument(reference, false);
                }
                _locationReferences.Add(new InlinedLocationReference(reference, _metadata.CurrentActivity));
            }
        }

        // Scenarios like VBV/R needs to know if they should run their own compiler
        // during CacheMetadata.  If we find a compiled expression root, means we're  
        // already compiled. So set the IsStaticallyCompiled flag to true
        bool foundCompiledExpressionRoot = TryGetCompiledExpressionRootAtDesignTime(_expressionActivity,
            _metadataRoot,
            out _compiledRoot,
            out _expressionId);

        if (foundCompiledExpressionRoot)
        {
            _metadata.Environment.CompileExpressions = true;
            // For compiled C# expressions we create temp auto generated arguments
            // for all locations whether they are used in the expressions or not.
            // The TryGetReferenceToPublicLocation method call above also generates
            // temp arguments for all locations. 
            // However for VB expressions, this leads to inconsistency between build
            // time and run time as during build time VB only generates temp arguments
            // for locations that are referenced in the expressions. To maintain 
            // consistency the we call the CreateRequiredArguments method seperately to
            // generates auto arguments only for locations that are referenced.
            if (!requiresCompilation)
            {
                IList<string> requiredLocationNames = _compiledRoot.GetRequiredLocations(_expressionId);
                CreateRequiredArguments(requiredLocationNames);
            }
        }
    }

    private bool TryGetCompiledExpressionRootAtDesignTime(Activity expression, Activity target, out ICompiledExpressionRoot compiledExpressionRoot, out int exprId)
    {
        if (!TryGetCompiledExpressionRoot(expression, target, out compiledExpressionRoot) ||
            !CanExecuteExpression(compiledExpressionRoot, out exprId))
        {
            return FindCompiledExpressionRoot(out exprId, out compiledExpressionRoot);
        }

        return true;
    }

    private bool FindCompiledExpressionRoot(out int exprId, out ICompiledExpressionRoot compiledExpressionRoot)
    {
        Activity root = _metadata.CurrentActivity.Parent;

        while (root != null)
        {
            if (TryGetCompiledExpressionRoot(_metadata.CurrentActivity, root, out ICompiledExpressionRoot currentCompiledExpressionRoot))
            {
                if (CanExecuteExpression(currentCompiledExpressionRoot, out exprId))
                {
                    compiledExpressionRoot = currentCompiledExpressionRoot;
                    return true;
                }
            }
            root = root.Parent;
        }

        exprId = -1;
        compiledExpressionRoot = null;

        return false;
    }

    private void CreateRequiredArguments(IList<string> requiredLocationNames)
    {
        LocationReference reference;
        if (requiredLocationNames != null && requiredLocationNames.Count > 0)
        {
            foreach (string name in requiredLocationNames)
            {
                reference = FindLocationReference(name);
                if (reference != null)
                {
                    if (_isReference)
                    {
                        _accessor.CreateLocationArgument(reference, true);
                    }
                    else
                    {
                        _accessor.CreateArgument(reference, ArgumentDirection.In, true);
                    }
                }
            }
        }
    }

    private LocationReference FindLocationReference(string name)
    {
        LocationReference returnValue = null;

        LocationReferenceEnvironment current = _accessor.ActivityMetadata.Environment;
        while (current != null)
        {
            if (current.TryGetLocationReference(name, out returnValue))
            {
                return returnValue;
            }
            current = current.Parent;
        }

        return returnValue;
    }
}
