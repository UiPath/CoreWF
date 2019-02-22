// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal class FaultContext
    {
        private Exception exception;
        private ActivityInstanceReference source;

        internal FaultContext(Exception exception, ActivityInstanceReference sourceReference)
        {
            Fx.Assert(exception != null, "Must have an exception.");
            Fx.Assert(sourceReference != null, "Must have a source.");

            this.Exception = exception;
            this.Source = sourceReference;
        }
        
        public Exception Exception
        {
            get
            {
                return this.exception;
            }
            private set
            {
                this.exception = value;
            }
        }
        
        public ActivityInstanceReference Source
        {
            get
            {
                return this.source;
            }
            private set
            {
                this.source = value;
            }
        }

        //[DataMember(Name = "Exception")]
        internal Exception SerializedException
        {
            get { return this.Exception; }
            set { this.Exception = value; }
        }
        [DataMember(Name = "Source")]
        internal ActivityInstanceReference SerializedSource
        {
            get { return this.Source; }
            set { this.Source = value; }
        }
    }
}


