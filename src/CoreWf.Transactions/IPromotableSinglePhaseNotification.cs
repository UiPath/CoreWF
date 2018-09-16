// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.


namespace CoreWf.Transactions
{
    public interface IPromotableSinglePhaseNotification : ITransactionPromoter
    {
        void Initialize();

        void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment);

        void Rollback(SinglePhaseEnlistment singlePhaseEnlistment);
    }
}
