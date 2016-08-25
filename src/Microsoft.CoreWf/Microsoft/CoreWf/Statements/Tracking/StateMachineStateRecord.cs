// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Tracking;
using System;
using System.Diagnostics.Tracing;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Statements.Tracking
{
    /// <summary>
    /// Represents a tracking record that is created when an state machine instance transitions to a state.
    /// </summary>
    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class StateMachineStateRecord : CustomTrackingRecord
    {
        internal static readonly string StateMachineStateRecordName = "Microsoft.CoreWf.Statements.StateMachine";

        private const string StateKey = "currentstate";
        private const string StateMachineKey = "stateMachine";

        /// <summary>
        /// Initializes a new instance of the StateMachineStateRecord class.
        /// </summary>
        public StateMachineStateRecord()
            : this(StateMachineStateRecordName)
        {
        }

        // Disable the user from arbitrary specifying a name for StateMachine specific tracking record.
        internal StateMachineStateRecord(string name)
            : base(name)
        {
        }

        internal StateMachineStateRecord(string name, EventLevel level)
            : base(name, level)
        {
        }

        internal StateMachineStateRecord(Guid instanceId, string name, EventLevel level)
            : base(instanceId, name, level)
        {
        }

        private StateMachineStateRecord(StateMachineStateRecord record)
            : base(record)
        {
        }

        /// <summary>
        /// Gets the display name of the State Machine activity that contains the state.
        /// </summary>
        public string StateMachineName
        {
            get
            {
                if (Data.ContainsKey(StateMachineKey))
                {
                    return Data[StateMachineKey].ToString();
                }

                return string.Empty;
            }

            internal set
            {
                Data[StateMachineKey] = value;
            }
        }

        /// <summary>
        /// Gets the display name of executing state when the record is generated.
        /// </summary>
        [DataMember]
        public string StateName
        {
            get
            {
                if (Data.ContainsKey(StateKey))
                {
                    return Data[StateKey].ToString();
                }

                return string.Empty;
            }

            internal set
            {
                Data[StateKey] = value;
            }
        }

        /// <summary>
        /// Creates a copy of the StateMachineTrackingRecord. (Overrides CustomTrackingRecord.Clone().)
        /// </summary>
        /// <returns>A copy of the StateMachineTrackingRecord instance.</returns>
        protected internal override TrackingRecord Clone()
        {
            return new StateMachineStateRecord(this);
        }
    }
}
