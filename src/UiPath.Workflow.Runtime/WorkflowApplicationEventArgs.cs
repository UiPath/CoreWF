// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Runtime;

[Fx.Tag.XamlVisible(false)]
public class WorkflowApplicationEventArgs : EventArgs
{
    internal WorkflowApplicationEventArgs(WorkflowApplication application) => Owner = application;

    internal WorkflowApplication Owner { get; private set; }

    public Guid InstanceId => Owner.Id;

    public IEnumerable<T> GetInstanceExtensions<T>()
        where T : class => Owner.InternalGetExtensions<T>();
}
