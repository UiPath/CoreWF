// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace CoreWf.Runtime.DurableInstancing
{
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.FlagsEnumsShouldHavePluralNames, //Justification = "Consistency is an adjective.")]
    [Flags]
    public enum InstanceValueConsistency
    {
        None = 0,
        InDoubt = 1,
        Partial = 2,
    }
}
