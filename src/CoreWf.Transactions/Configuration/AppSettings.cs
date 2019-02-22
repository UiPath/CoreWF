// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.


namespace System.Activities.Transactions.Configuration
{
    internal static class AppSettings
    {
        private static readonly bool s_settingsInitalized = false;
        private static readonly object s_appSettingsLock = new object();
        private static bool s_includeDistributedTxIdInExceptionMessage;

        private static void EnsureSettingsLoaded()
        {
            if (!s_settingsInitalized)
            {
                lock (s_appSettingsLock)
                {
                    if (!s_settingsInitalized)
                    {
                        // TODO: Determine how to handle configuration.
                        // This uses System.Configuration on desktop to load:
                        // Transactions:IncludeDistributedTransactionIdInExceptionMessage
                        s_includeDistributedTxIdInExceptionMessage = false;
                    }
                }
            }
        }

        internal static bool IncludeDistributedTxIdInExceptionMessage
        {
            get
            {
                EnsureSettingsLoaded();
                return s_includeDistributedTxIdInExceptionMessage;
            }
        }
    }
}
