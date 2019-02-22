// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Tracking
{
    using System.Collections.ObjectModel;

    public class ActivityStateQuery : TrackingQuery
    {
        private Collection<string> arguments;
        private Collection<string> states;
        private Collection<string> variables;        
        
        public ActivityStateQuery()
        {
            this.ActivityName = "*";
        }

        public string ActivityName
        {
            get;
            set;
        }

        public Collection<string> Arguments
        {
            get
            {
                if (this.arguments == null)
                {
                    this.arguments = new Collection<string>();
                }

                return this.arguments;
            }
        }

        public Collection<string> Variables
        {
            get
            {
                if (this.variables == null)
                {
                    this.variables = new Collection<string>();
                }

                return this.variables;
            }
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

        internal bool HasArguments
        {
            get
            {
                return this.arguments != null && this.arguments.Count > 0;
            }
        }

        internal bool HasVariables
        {
            get
            {
                return this.variables != null && this.variables.Count > 0;
            }
        }        
    }
}
