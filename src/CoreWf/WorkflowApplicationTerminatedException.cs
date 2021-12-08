// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

[Serializable]
public class WorkflowApplicationTerminatedException : WorkflowApplicationCompletedException
{
    public WorkflowApplicationTerminatedException()
    {
    }

    public WorkflowApplicationTerminatedException(string message)
        : base(message)
    {
    }

    public WorkflowApplicationTerminatedException(string message, Guid instanceId)
        : base(message, instanceId)
    {
    }

    public WorkflowApplicationTerminatedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public WorkflowApplicationTerminatedException(string message, Guid instanceId, Exception innerException)
        : base(message, instanceId, innerException)
    {
    }

    protected WorkflowApplicationTerminatedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
