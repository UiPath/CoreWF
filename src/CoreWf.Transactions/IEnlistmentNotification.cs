// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.


namespace System.Activities.Transactions
{
    internal interface IEnlistmentNotificationInternal
    {
        void Prepare(IPromotedEnlistment preparingEnlistment);

        void Commit(IPromotedEnlistment enlistment);

        void Rollback(IPromotedEnlistment enlistment);

        void InDoubt(IPromotedEnlistment enlistment);
    }

    public interface IEnlistmentNotification
    {
        void Prepare(PreparingEnlistment preparingEnlistment);

        void Commit(Enlistment enlistment);

        void Rollback(Enlistment enlistment);

        void InDoubt(Enlistment enlistment);
    }
}
