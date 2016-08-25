// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Microsoft.CoreWf.Tracking
{
    public class ActivityStateQuery : TrackingQuery
    {
        private Collection<string> _arguments;
        private Collection<string> _states;
        private Collection<string> _variables;

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
                if (_arguments == null)
                {
                    _arguments = new Collection<string>();
                }

                return _arguments;
            }
        }

        public Collection<string> Variables
        {
            get
            {
                if (_variables == null)
                {
                    _variables = new Collection<string>();
                }

                return _variables;
            }
        }

        public Collection<string> States
        {
            get
            {
                if (_states == null)
                {
                    _states = new Collection<string>();
                }
                return _states;
            }
        }

        internal bool HasStates
        {
            get
            {
                return _states != null && _states.Count > 0;
            }
        }

        internal bool HasArguments
        {
            get
            {
                return _arguments != null && _arguments.Count > 0;
            }
        }

        internal bool HasVariables
        {
            get
            {
                return _variables != null && _variables.Count > 0;
            }
        }
    }
}
