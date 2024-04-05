namespace System.Activities.Statements.Interfaces;

/* Public interfaces for flowchart components. 
 * The abstractions are used for handling external flowchart implementations. 
 * This project provides the default implementation.*/

/// <summary>
/// Base interface for all flow chart nodes
/// </summary>
public interface IFlowNode 
{
    IEnumerable<IFlowNode> GetConnectedNodes();
}

/// <summary>
/// flowchart abstraction
/// </summary>
public interface IFlowchart: IHasVariables
{ 
    IFlowNode StartNode { get; set; }

    IEnumerable<IFlowNode> Nodes { get; }
}

/// <summary>
/// flow decision abstraction
/// </summary>
public interface IFlowDecision: IFlowNode, IHasDisplayName, IHasCondition 
{ 
    public IFlowNode True { get; set; }

    public IFlowNode False { get; set; }
}

/// <summary>
/// flow step abstraction
/// </summary>
public interface IFlowStep: IFlowNode, IHasAction
{ 
    IFlowNode Next { get; set; }
}

/// <summary>
/// Part of the flow switch abstraction that doesn't depend on the generic type
/// </summary>
public interface IFlowSwitch: IFlowNode, IHasDisplayName, IHasExpressionNonGeneric { 

    IFlowNode Default { get; set; }

    IEnumerable<IFlowNode> CaseNodes { get; }
}

/// <summary>
/// flow switch abstraction, including the condition type
/// </summary>
/// <typeparam name="T">The condition type</typeparam>
public interface IFlowSwitch<T>: IFlowSwitch, IHasExpression<T>
{
    IReadOnlyDictionary<T, IFlowNode> Cases { get; }
}

/// <summary>
/// Attribute used at design time to discover the implementation flow step type.
/// Apply this to the flow chart implementation type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class FlowStepTypeAttribute: Attribute
{
    public FlowStepTypeAttribute(Type flowStepType)
    {
        FlowStepType = flowStepType;
    }

    public Type FlowStepType { get; }
}
