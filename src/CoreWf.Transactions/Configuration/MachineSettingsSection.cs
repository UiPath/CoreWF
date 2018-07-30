// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace CoreWf.Transactions.Configuration
{
    internal sealed class MachineSettingsSection // ConfigurationSection
    {
        private static readonly MachineSettingsSection s_section = new MachineSettingsSection();
        private static TimeSpan s_maxTimeout = TimeSpan.Parse(ConfigurationStrings.DefaultMaxTimeout);

        internal static MachineSettingsSection GetSection() => s_section;

        public TimeSpan MaxTimeout
        {
            get { return s_maxTimeout; }
            set
            {
                if (value < TimeSpan.Zero || value > TimeSpan.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxTimeout), SR.ConfigInvalidTimeSpanValue);
                }
                s_maxTimeout = value;
            }
        }
    }
}
