using System.Collections.ObjectModel;

namespace System.Activities.Statements.Interfaces;

/// <summary>
/// Describes the property of having a child activity that is executed as action
/// </summary>
public interface IHasAction
{
    Activity Action { get; set; }
}

/// <summary>
/// Describes the property of having a child activity that is evaluated as condition
/// </summary>
public interface IHasCondition
{
    Activity<bool> Condition { get; set; }
}

public interface IHasDisplayName
{
    string DisplayName { get; set; }
}

/// <summary>
/// Describes the property of having an expression with known result ype
/// </summary>
public interface IHasExpression<T>: IHasExpressionNonGeneric
{
    public Activity<T> Expression { get; set; }
}

/// <summary>
/// Describes the property of having an opaque expression (unknown result type)
/// </summary>
public interface IHasExpression
{
    public Activity Expression { get; set; }
}

/// <summary>
/// Same as <see cref="IHasExpression"/>, but implemented by <see cref="IHasExpression{T}"/> without property name conflict
/// </summary>
public interface IHasExpressionNonGeneric
{
    public Activity ExpressionNonGeneric { get; }
}

/// <summary>
/// Describes the property of having a collection of variables
/// </summary>
public interface IHasVariables
{
    Collection<Variable> Variables { get; }
}
