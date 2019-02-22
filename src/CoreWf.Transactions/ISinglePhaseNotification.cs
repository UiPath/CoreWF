// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.


namespace System.Activities.Transactions
{
    internal interface ISinglePhaseNotificationInternal : IEnlistmentNotificationInternal
    {
        void SinglePhaseCommit(IPromotedEnlistment singlePhaseEnlistment);
    }

    public interface ISinglePhaseNotification : IEnlistmentNotification
    {
        void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment);
    }
}
