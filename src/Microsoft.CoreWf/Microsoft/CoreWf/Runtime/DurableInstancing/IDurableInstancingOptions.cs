// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;

namespace Microsoft.CoreWf.Runtime.DurableInstancing
{
    //This sole purpose of this interface is to avoid adding S.SM.Activation as a friend of S.SM.Activities
    internal interface IDurableInstancingOptions
    {
        void SetScopeName(XName scopeName);
    }
}
