// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Hosting
{
    using System.Collections.Generic;

    // overriden by extensions that want to contribute additional
    // extensions and/or get notified when they are being used with a WorkflowInstance
    public interface IWorkflowInstanceExtension
    {
        IEnumerable<object> GetAdditionalExtensions();

        // called with the targe instance under WorkflowInstance.Initialize
        void SetInstance(WorkflowInstanceProxy instance);
    }
}
