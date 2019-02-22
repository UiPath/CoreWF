// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace System.Activities.Transactions
{
    internal static class SR
    {
        private static readonly IStringLocalizer localizer;

        static SR()
        {
            var locOptions = new LocalizationOptions() { ResourcesPath = "resources" };
            var options = Options.Create<LocalizationOptions>(locOptions);
            var resourceFactory = new ResourceManagerStringLocalizerFactory(options, NullLoggerFactory.Instance);
            localizer = resourceFactory.Create(typeof(SR));
        }

        internal static string AsyncFlowAndESInteropNotSupported => localizer["TransactionScope with TransactionScopeAsyncFlowOption.Enabled option is not supported when the TransactionScope is used within Enterprise Service context with Automatic or Full EnterpriseServicesInteropOption enabled in parent scope."].Value;
        internal static string BadAsyncResult => localizer["The IAsyncResult parameter must be the same parameter returned by BeginCommit."].Value;
        internal static string BadResourceManagerId => localizer["Resource Manager Identifiers cannot be Guid.Empty."].Value;
        internal static string CannotPromoteSnapshot => localizer["Transactions with IsolationLevel Snapshot cannot be promoted."].Value;
        internal static string CannotSetCurrent => localizer["Current cannot be set directly when Com+ Interop is enabled."].Value;
        internal static string ConfigInvalidTimeSpanValue => localizer["Valid TimeSpan values are greater than TimeSpan.Zero."].Value;
        internal static string CurrentDelegateSet => localizer["The delegate for an external current can only be set once."].Value;
        internal static string DisposeScope => localizer["The current TransactionScope is already complete. You should dispose the TransactionScope."].Value;
        internal static string EnlistmentStateException => localizer["The operation is not valid for the current state of the enlistment."].Value;
        internal static string EsNotSupported => localizer["Com+ Interop features cannot be supported."].Value;
        internal static string InternalError => localizer["Internal Error"].Value;
        internal static string InvalidArgument => localizer["The argument is invalid."].Value;
        internal static string InvalidIPromotableSinglePhaseNotificationSpecified => localizer["The specified IPromotableSinglePhaseNotification is not the same as the one provided to EnlistPromotableSinglePhase."].Value;
        internal static string InvalidRecoveryInformation => localizer["Transaction Manager in the Recovery Information does not match the configured transaction manager."].Value;
        internal static string InvalidScopeThread => localizer["A TransactionScope must be disposed on the same thread that it was created."].Value;
        internal static string PromotionFailed => localizer["There was an error promoting the transaction to a distributed transaction."].Value;
        internal static string PromotedReturnedInvalidValue => localizer["The Promote method returned an invalid value for the distributed transaction."].Value;
        internal static string PromotedTransactionExists => localizer["The transaction returned from Promote already exists as a distributed transaction."].Value;
        internal static string TooLate => localizer["It is too late to add enlistments to this transaction."].Value;
        internal static string TraceTransactionTimeout => localizer["Transaction Timeout"].Value;
        internal static string TransactionAborted => localizer["The transaction has aborted."].Value;
        internal static string TransactionAlreadyCompleted => localizer["DependentTransaction.Complete or CommittableTransaction.Commit has already been called for this transaction."].Value;
        internal static string TransactionIndoubt => localizer["The transaction is in doubt."].Value;
        internal static string TransactionManagerCommunicationException => localizer["Communication with the underlying transaction manager has failed."].Value;
        internal static string TransactionScopeComplete => localizer["The current TransactionScope is already complete."].Value;
        internal static string TransactionScopeIncorrectCurrent => localizer["Transaction.Current has changed inside of the TransactionScope."].Value;
        internal static string TransactionScopeInvalidNesting => localizer["TransactionScope nested incorrectly."].Value;
        internal static string TransactionScopeIsolationLevelDifferentFromTransaction => localizer["The transaction specified for TransactionScope has a different IsolationLevel than the value requested for the scope."].Value;
        internal static string TransactionScopeTimerObjectInvalid => localizer["TransactionScope timer object is invalid."].Value;
        internal static string TransactionStateException => localizer["The operation is not valid for the state of the transaction."].Value;
        internal static string UnexpectedFailureOfThreadPool => localizer["There was an unexpected failure of QueueUserWorkItem."].Value;
        internal static string UnexpectedTimerFailure => localizer["There was an unexpected failure of a timer."].Value;
        internal static string UnrecognizedRecoveryInformation => localizer["The RecoveryInformation provided is not recognized by this version of System.Transactions."].Value;
        internal static string VolEnlistNoRecoveryInfo => localizer["Volatile enlistments do not generate recovery information."].Value;
        internal static string DistributedTxIDInTransactionException(params object[] args) => localizer["{0} Distributed Transaction ID is {1}", args].Value;
        internal static string PromoterTypeInvalid => localizer["The specified PromoterType is invalid."].Value;
        internal static string PromoterTypeUnrecognized(params object[] args) => localizer["There is a promotable enlistment for the transaction which has a PromoterType value that is not recognized by System.Transactions. {0}", args].Value;
        internal static string DistributedNotSupported => localizer["This platform does not support distributed transactions."].Value;
    
    }
}

