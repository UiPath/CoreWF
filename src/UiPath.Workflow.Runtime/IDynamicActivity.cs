// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities;

public interface IDynamicActivity
{
    string Name { get; set; }
    KeyedCollection<string, DynamicActivityProperty> Properties { get; }
}
