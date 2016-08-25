// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.CoreWf
{
    internal interface IInstanceNotificationListener
    {
        void AbortInstance(Exception reason, bool isWorkflowThread);
        void OnIdle();
        bool OnUnhandledException(Exception exception, Activity exceptionSource);
    }
}
