// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    internal interface IFlowSwitch
    {
        bool Execute(NativeActivityContext context, Flowchart parent);
        FlowNode GetNextNode(object value);
    }
}
