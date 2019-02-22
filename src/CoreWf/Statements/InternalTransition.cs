// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System.Activities;
    using System.Collections.ObjectModel;

    /// <summary>
    /// InternalTransition is internal representation of transition.
    /// Its difference from transition is that if several transition share the same trigger, all of them belongs to the same internal transition.
    /// Their different conditions, actions, Tos would be put into TransitionDataList.
    /// </summary>
    internal sealed class InternalTransition
    {
        private Collection<TransitionData> transitionDataList;

        /// <summary>
        /// Gets or sets the index of this InternalTransition in internalTransitions list of its parent state.
        /// </summary>
        public int InternalTransitionIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this transition is unconditional.
        /// </summary>
        public bool IsUnconditional
        {
            get
            {
                return this.transitionDataList.Count == 1 && this.transitionDataList[0].Condition == null;
            }
        }

        /// <summary>
        /// Gets TransitionDataList contains Tos, Conditions, Actions of different transitions which share the same trigger.
        /// </summary>
        public Collection<TransitionData> TransitionDataList
        {
            get
            {
                if (this.transitionDataList == null)
                {
                    this.transitionDataList = new Collection<TransitionData>();
                }

                return this.transitionDataList;
            }
        }

        /// <summary>
        /// Gets or sets trigger object of this internal transition.
        /// </summary>
        public Activity Trigger
        {
            get;
            set;
        }
    }
}
