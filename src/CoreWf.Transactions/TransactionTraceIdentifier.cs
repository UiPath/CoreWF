// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace System.Activities.Transactions
{
    /// <summary>
    /// This identifier is used in tracing to distinguish instances
    /// of transaction objects.  This identifier is only unique within
    /// a given AppDomain.
    /// </summary>
    internal struct TransactionTraceIdentifier : IEquatable<TransactionTraceIdentifier>
    {
        public static readonly TransactionTraceIdentifier Empty = new TransactionTraceIdentifier();

        public TransactionTraceIdentifier(string transactionIdentifier, int cloneIdentifier)
        {
            _transactionIdentifier = transactionIdentifier;
            _cloneIdentifier = cloneIdentifier;
        }

        private readonly string _transactionIdentifier;
        /// <summary>
        /// The string representation of the transaction identifier.
        /// </summary>
        public string TransactionIdentifier => _transactionIdentifier;

        private readonly int _cloneIdentifier;
        /// <summary>
        /// An integer value that allows different clones of the same
        /// transaction to be distinguished in the tracing.
        /// </summary>
        public int CloneIdentifier => _cloneIdentifier;

        public override int GetHashCode() => base.GetHashCode();  // Don't have anything better to do.

        public override bool Equals(object obj) => obj is TransactionTraceIdentifier && Equals((TransactionTraceIdentifier)obj);

        public bool Equals(TransactionTraceIdentifier other) =>
            _cloneIdentifier == other._cloneIdentifier &&
            _transactionIdentifier == other._transactionIdentifier;

        public static bool operator ==(TransactionTraceIdentifier left, TransactionTraceIdentifier right) => left.Equals(right);

        public static bool operator !=(TransactionTraceIdentifier left, TransactionTraceIdentifier right) => !left.Equals(right);
    }
}
