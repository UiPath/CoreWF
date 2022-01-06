// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.XamlIntegration;
using System.Linq.Expressions;
using System.Windows.Markup;

namespace System.Activities.Expressions;

// consciously not XAML-friendly since Linq Expressions aren't create-set-use
[Fx.Tag.XamlVisible(false)]
[Diagnostics.DebuggerStepThrough]
public sealed class LambdaReference<T> : CodeActivity<Location<T>>, IExpressionContainer, IValueSerializableExpression
{
    private readonly Expression<Func<ActivityContext, T>> _locationExpression;
    private Expression<Func<ActivityContext, T>> _rewrittenTree;
    private LocationFactory<T> _locationFactory;

    public LambdaReference(Expression<Func<ActivityContext, T>> locationExpression)
    {
        _locationExpression = locationExpression ?? throw FxTrace.Exception.ArgumentNull(nameof(locationExpression));
        UseOldFastPath = true;
    }

    // this is called via reflection from Microsoft.CDF.Test.ExpressionUtilities.Activities.ActivityUtilities.ReplaceLambdaValuesInActivityTree
    internal Expression LambdaExpression => _locationExpression;

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        CodeActivityPublicEnvironmentAccessor publicAccessor = CodeActivityPublicEnvironmentAccessor.Create(metadata);

        // We need to rewrite the tree.
        if (ExpressionUtilities.TryRewriteLambdaExpression(_locationExpression, out Expression newTree, publicAccessor, true))
        {
            _rewrittenTree = (Expression<Func<ActivityContext, T>>)newTree;
        }
        else
        {
            _rewrittenTree = _locationExpression;
        }

        // inspect the expressionTree to see if it is a valid location expression(L-value)
        if (!ExpressionUtilities.IsLocation(_rewrittenTree, typeof(T), out string extraErrorMessage))
        {
            string errorMessage = SR.InvalidLValueExpression;
            if (extraErrorMessage != null)
            {
                errorMessage += ":" + extraErrorMessage;
            }
            metadata.AddValidationError(errorMessage);
        }
    }

    protected override Location<T> Execute(CodeActivityContext context)
    {
        _locationFactory ??= ExpressionUtilities.CreateLocationFactory<T>(_rewrittenTree);
        return _locationFactory.CreateLocation(context);
    }

    public bool CanConvertToString(IValueSerializerContext context) => true;

    public string ConvertToString(IValueSerializerContext context) =>
        // This workflow contains lambda expressions specified in code. 
        // These expressions are not XAML serializable. 
        // In order to make your workflow XAML-serializable, 
        // use either VisualBasicValue/Reference or ExpressionServices.Convert  
        // This will convert your lambda expressions into expression activities.
        throw FxTrace.Exception.AsError(new LambdaSerializationException());
}
