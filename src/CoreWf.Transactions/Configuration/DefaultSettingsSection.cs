// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace System.Activities.Transactions.Configuration
{
    internal sealed class DefaultSettingsSection // ConfigurationSection
    {
        private static readonly DefaultSettingsSection s_section = new DefaultSettingsSection();
        private static TimeSpan s_timeout = TimeSpan.Parse(ConfigurationStrings.DefaultTimeout);

        internal static DefaultSettingsSection GetSection() => s_section;

        public string DistributedTransactionManagerName { get; set; } = ConfigurationStrings.DefaultDistributedTransactionManagerName;

        public TimeSpan Timeout
        {
            get { return s_timeout; }
            set
            {
                if (value < TimeSpan.Zero || value > TimeSpan.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(Timeout), SR.ConfigInvalidTimeSpanValue);
                }
                s_timeout = value;
            }
        }
    }
}
