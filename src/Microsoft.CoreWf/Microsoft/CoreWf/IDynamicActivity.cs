// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace CoreWf
{
    internal interface IDynamicActivity
    {
        string Name { get; set; }
        KeyedCollection<string, DynamicActivityProperty> Properties { get; }
    }
}
