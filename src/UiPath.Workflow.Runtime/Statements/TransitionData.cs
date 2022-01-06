// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements;

/// <summary>
/// TransitionData is used by InternalTransition to store data from Transition.
/// </summary>
internal sealed class TransitionData
{
    /// <summary>
    /// Gets or sets Action of transition.
    /// </summary>
    public Activity Action { get; set; }

    /// <summary>
    /// Gets or sets Condition of transition.
    /// If condition is null, it means it's an unconditional transition.
    /// </summary>
    public Activity<bool> Condition { get; set; }

    /// <summary>
    /// Gets or sets To of transition, which represent the target InternalState.
    /// </summary>
    public InternalState To { get; set; }
}
