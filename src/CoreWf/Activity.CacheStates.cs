// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public abstract partial class Activity
{
    private enum CacheStates : byte
    {
        // We don't have valid cached data
        Uncached = 0x00,

        // The next two states are mutually exclusive:

        // The activity has its own metadata cached, or private implementation are skipped
        Partial = 0x01,

        // The activity has its own metadata and its private implementation cached
        // We can make use of the roll-up metadata (like
        // SubtreeHasConstraints).
        Full = 0x02,

        // The next state can be ORed with the last two:

        // The cached data is ready for runtime use
        RuntimeReady = 0x04
    }
}
