// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Microsoft.CoreWf.Tracking
{
    public class WorkflowInstanceQuery : TrackingQuery
    {
        private Collection<string> _states;

        public WorkflowInstanceQuery()
        {
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
    }
}
