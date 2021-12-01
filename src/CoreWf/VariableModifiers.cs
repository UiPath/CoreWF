// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

[Flags]
public enum VariableModifiers
{
    None = 0X00,
    ReadOnly = 0X01,
    Mapped = 0X02        
}
