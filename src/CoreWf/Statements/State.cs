// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Statements;

/// <summary>
/// This class represents a State in a StateMachine.
/// </summary>
public sealed class State
{
    private InternalState _internalState;
    private Collection<Transition> _transitions;
    private NoOp _nullTrigger;
    private Collection<Variable> _variables;

    /// <summary>
    /// Gets or sets DisplayName of the State.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets entry action of the State. It is executed when the StateMachine enters the State. 
    /// It's optional.
    /// </summary>
    [DefaultValue(null)]
    public Activity Entry { get; set; }

    /// <summary>
    /// Gets or sets exit action of the State. It is executed when the StateMachine leaves the State. 
    /// It's optional.
    /// </summary>
    [DependsOn("Entry")]
    [DefaultValue(null)]
    public Activity Exit { get; set; }


    /// <summary>
    /// Gets Transitions collection contains all outgoing Transitions from the State.
    /// </summary>
    [DependsOn("Exit")]
    public Collection<Transition> Transitions
    {
        get
        {
            _transitions ??= new ValidatingCollection<Transition>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(item)));
                    }
                },
            };
            return _transitions;
        }
    }

    /// <summary>
    /// Gets Variables which can be used within the scope of State and its Transitions collection.
    /// </summary>
    [DependsOn("Transitions")]
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
                        throw FxTrace.Exception.AsError(new ArgumentNullException(nameof(item)));
                    }
                },
            };
            return _variables;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the State is a final State.
    /// </summary>
    [DefaultValue(false)]
    public bool IsFinal { get; set; }

    /// <summary>
    /// Gets Internal activity representation of state.
    /// </summary>
    internal InternalState InternalState
    {
        get
        {
            _internalState ??= new InternalState(this);
            return _internalState;
        }
    }

    /// <summary>
    /// Gets or sets PassNumber is used to detect re-visiting when traversing states in StateMachine. 
    /// </summary>
    internal uint PassNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether state can be reached via transitions.
    /// </summary>
    internal bool Reachable { get; set; }

    /// <summary>
    /// Gets or sets StateId is unique within a StateMachine.
    /// </summary>
    internal string StateId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the parent state machine of the state.
    /// Used for tracking purpose only.
    /// </summary>
    internal string StateMachineName { get; set; }

    /// <summary>
    /// Clear internal state. 
    /// </summary>
    internal void ClearInternalState() => _internalState = null;

    internal NoOp NullTrigger
    {
        get
        {
            _nullTrigger ??= new NoOp
            {
                DisplayName = "Null Trigger"
            };
            return _nullTrigger;
        }
    }

    internal sealed class NoOp : CodeActivity
    {
        protected override void Execute(CodeActivityContext context) { }
    }
}
