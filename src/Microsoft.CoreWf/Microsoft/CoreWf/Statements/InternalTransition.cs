// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace CoreWf.Statements
{
    /// <summary>
    /// InternalTransition is internal representation of transition.
    /// Its difference from transition is that if several transition share the same trigger, all of them belongs to the same internal transition.
    /// Their different conditions, actions, Tos would be put into TransitionDataList.
    /// </summary>
    internal sealed class InternalTransition
    {
        private Collection<TransitionData> _transitionDataList;

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
                return _transitionDataList.Count == 1 && _transitionDataList[0].Condition == null;
            }
        }

        /// <summary>
        /// Gets TransitionDataList contains Tos, Conditions, Actions of different transitions which share the same trigger.
        /// </summary>
        public Collection<TransitionData> TransitionDataList
        {
            get
            {
                if (_transitionDataList == null)
                {
                    _transitionDataList = new Collection<TransitionData>();
                }

                return _transitionDataList;
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
