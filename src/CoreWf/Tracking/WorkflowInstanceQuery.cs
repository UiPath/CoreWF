// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking
{
    using System.Collections.ObjectModel;

    public class WorkflowInstanceQuery : TrackingQuery
    {
        private Collection<string> states;

        public WorkflowInstanceQuery()
        {
        }

        public Collection<string> States
        {
            get
            {
                if (this.states == null)
                {
                    this.states = new Collection<string>();
                }
                return this.states;
            }
        }

        internal bool HasStates
        {
            get
            {
                return this.states != null && this.states.Count > 0;
            }
        }

    }
}
