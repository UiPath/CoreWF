// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Linq.Expressions;

namespace System.Activities.XamlIntegration;

internal class ExpressionTreeRewriter : ExpressionVisitor
{
    private readonly IList<LocationReference> _locationReferences;

    public ExpressionTreeRewriter() { }

    public ExpressionTreeRewriter(IList<LocationReference> locationReferences)
    {
        _locationReferences = locationReferences;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null && node.Expression.NodeType == ExpressionType.Constant)
        {
            ConstantExpression constExpr = (ConstantExpression)node.Expression;
            if (typeof(CompiledDataContext).IsAssignableFrom(constExpr.Type) &&
                TryRewriteMemberExpressionNode(node, out Expression newNode))
            {
                return newNode;
            }
        }

        return base.VisitMember(node);
    }       

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value != null && node.Value.GetType() == typeof(InlinedLocationReference))
        {
            ILocationReferenceWrapper inlinedReference = (ILocationReferenceWrapper)node.Value;
            if (inlinedReference != null)
            {
                Expression newNode = Expression.Constant(inlinedReference.LocationReference, typeof(LocationReference));
                return newNode;
            }
        }

        return base.VisitConstant(node);
    }

    private bool TryRewriteMemberExpressionNode(MemberExpression node, out Expression newNode)
    {
        newNode = null;
        if (_locationReferences != null)
        {
            foreach (LocationReference locationReference in _locationReferences)
            {
                if (node.Member.Name == locationReference.Name && node.Type == locationReference.Type)
                {
                    if (locationReference is ILocationReferenceWrapper wrapper)
                    {
                        newNode = ExpressionUtilities.CreateIdentifierExpression(wrapper.LocationReference);
                    }
                    else
                    {
                        newNode = ExpressionUtilities.CreateIdentifierExpression(locationReference);
                    }
                    return true;
                }
            }
        }

        return false;
    }
}
