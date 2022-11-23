using System.Activities;
using System.Activities.Statements;
using Xunit;
namespace TestCases.Activities;
public class StateMachineActivity
{
    [Fact]
    public void MultipleTransitions()
    {
        State finalState = new() { Entry = new WriteLine { Text = "finalState" }, IsFinal = true };
        State state1 = new() { Entry = new WriteLine { Text = "state1" }, Transitions = { new() { To = finalState } } };
        State state2 = new() { Entry = new WriteLine { Text = "state2" }, Transitions = { new() { To = finalState } } };
        State initialState = new() { Entry = new WriteLine { Text = "initialState" }, Transitions = { new() { To = state1, Condition = true }, new() { To = state2, Condition = true } } };
        StateMachine stateMachine = new() { InitialState = initialState, States = { initialState, state1, state2, finalState } };
        WorkflowInvoker.Invoke(stateMachine);
    }
}