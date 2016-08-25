// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal class FaultContext
    {
        private Exception _exception;
        private ActivityInstanceReference _source;

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
                return _exception;
            }
            private set
            {
                _exception = value;
            }
        }

        public ActivityInstanceReference Source
        {
            get
            {
                return _source;
            }
            private set
            {
                _source = value;
            }
        }

        [DataMember(Name = "Exception")]
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


