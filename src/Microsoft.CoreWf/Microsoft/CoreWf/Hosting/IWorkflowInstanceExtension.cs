// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.CoreWf.Hosting
{
    // overriden by extensions that want to contribute additional
    // extensions and/or get notified when they are being used with a WorkflowInstance
    public interface IWorkflowInstanceExtension
    {
        IEnumerable<object> GetAdditionalExtensions();

        // called with the targe instance under WorkflowInstance.Initialize
        void SetInstance(WorkflowInstanceProxy instance);
    }
}
