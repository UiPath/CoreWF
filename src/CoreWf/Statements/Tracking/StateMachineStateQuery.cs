// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements.Tracking
{
    using System.Activities.Tracking;

    /// <summary>
    /// When added to the Queries, subscribes to state machine state execution records.
    /// </summary>
    public sealed class StateMachineStateQuery : CustomTrackingQuery
    {
        /// <summary>
        /// Constructor of StateMachineTrackingQuery.
        /// </summary>
        public StateMachineStateQuery()
        {
            base.Name = StateMachineStateRecord.StateMachineStateRecordName;
        }   
        
        /// <summary>
        /// Gets the name that distinguishes this tracking record.
        /// </summary>
        public new string Name 
        {
            get
            {
                // By adding the 'new' keyword, the Name property appears to be overriden
                // and become a Get only property
                return base.Name;
            }
        }
    }
}   
