// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

[Flags]
public enum BookmarkOptions
{
    None = 0x00,
    MultipleResume = 0x01,
    NonBlocking = 0x02,
}
