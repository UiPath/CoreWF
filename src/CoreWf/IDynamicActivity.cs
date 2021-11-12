// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Collections.ObjectModel;

    public interface IDynamicActivity
    {
        string Name { get; set; }
        KeyedCollection<string, DynamicActivityProperty> Properties { get; }
    }
}
