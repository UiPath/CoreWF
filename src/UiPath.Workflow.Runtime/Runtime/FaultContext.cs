// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime;

[DataContract]
internal class FaultContext
{
    private Exception _exception;
    private ActivityInstanceReference _source;

    public FaultContext(Exception exception, ActivityInstanceReference sourceReference)
    {
        Fx.Assert(exception != null, "Must have an exception.");
        Fx.Assert(sourceReference != null, "Must have a source.");

        Exception = exception;
        Source = sourceReference;
    }

    public Exception Exception
    {
        get => _exception;
        private set => _exception = value;
    }

    public ActivityInstanceReference Source
    {
        get => _source;
        private set => _source = value;
    }

    //[DataMember(Name = "Exception")]
    internal Exception SerializedException
    {
        get => Exception;
        set => Exception = value;
    }
    [DataMember(Name = "Source")]
    internal ActivityInstanceReference SerializedSource
    {
        get => Source;
        set => Source = value;
    }
}
