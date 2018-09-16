// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using CoreWf;
    using System.Runtime.Serialization;

    /// <summary>
    /// TriggerCompletedEvent represents an event which is triggered when a trigger is completed.
    /// </summary>
    [DataContract]
    internal class TriggerCompletedEvent
    {
        /// <summary>
        /// Gets or sets Bookmark that starts evaluating condition(s).
        /// </summary>
        [DataMember]
        public Bookmark Bookmark
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets TriggerId, which is unique within a state
        /// </summary>
        [DataMember]
        public int TriggedId
        {
            get;
            set;
        }
    }
}
