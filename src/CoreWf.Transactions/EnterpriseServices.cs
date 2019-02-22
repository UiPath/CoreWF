// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace System.Activities.Transactions
{
    internal static class EnterpriseServices
    {
        internal static bool EnterpriseServicesOk => false;

        internal static void VerifyEnterpriseServicesOk()
        {
            if (!EnterpriseServicesOk)
            {
                ThrowNotSupported();
            }
        }

        internal static Transaction GetContextTransaction(ContextData contextData)
        {
            if (EnterpriseServicesOk)
            {
                ThrowNotSupported();
            }

            return null;
        }

        internal static bool CreatedServiceDomain { get; set; } = false;

        internal static bool UseServiceDomainForCurrent() => false;

        internal static void PushServiceDomain(Transaction newCurrent)
        {
            ThrowNotSupported();
        }

        internal static void LeaveServiceDomain()
        {
            ThrowNotSupported();
        }

        private static void ThrowNotSupported()
        {
            throw new PlatformNotSupportedException(SR.EsNotSupported);
        }
    }
}
