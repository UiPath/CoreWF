// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Statements
{
    [DataContract]
    /// <summary>
    /// TriggerCompletedEvent represents an event which is triggered when a trigger is completed.
    /// </summary>
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
