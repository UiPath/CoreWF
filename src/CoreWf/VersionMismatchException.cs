// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Security;

namespace System.Activities;
using Runtime;

[Serializable]
public class VersionMismatchException : Exception
{
    public VersionMismatchException()
        : base() { }

    public VersionMismatchException(string message)
        : base(message) { }

    public VersionMismatchException(string message, Exception innerException)
        : base(message, innerException) { }

    public VersionMismatchException(WorkflowIdentity expectedVersion, WorkflowIdentity actualVersion)
        : base(GetMessage(expectedVersion, actualVersion))
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public VersionMismatchException(string message, WorkflowIdentity expectedVersion, WorkflowIdentity actualVersion)
        : base(message)
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public VersionMismatchException(string message, WorkflowIdentity expectedVersion, WorkflowIdentity actualVersion, Exception innerException)
        : base(message, innerException)
    {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    protected VersionMismatchException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        ExpectedVersion = (WorkflowIdentity)info.GetValue("expectedVersion", typeof(WorkflowIdentity));
        ActualVersion = (WorkflowIdentity)info.GetValue("actualVersion", typeof(WorkflowIdentity));
    }

    public WorkflowIdentity ExpectedVersion { get; private set; }

    public WorkflowIdentity ActualVersion { get; private set; }

    [Fx.Tag.SecurityNote(Critical = "Critical because we are overriding a critical method in the base class.")]
    [SecurityCritical]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("expectedVersion", ExpectedVersion);
        info.AddValue("actualVersion", ActualVersion);
    }

    private static string GetMessage(WorkflowIdentity expectedVersion, WorkflowIdentity actualVersion)
    {
        if (actualVersion == null && expectedVersion != null)
        {
            return SR.WorkflowIdentityNullStateId(expectedVersion);
        }
        else if (actualVersion != null && expectedVersion == null)
        {
            return SR.WorkflowIdentityNullHostId(actualVersion);
        }
        else if (!Equals(expectedVersion, actualVersion))
        {
            return SR.WorkflowIdentityStateIdHostIdMismatch(actualVersion, expectedVersion);
        }
        else
        {
            return null;
        }
    }
}
