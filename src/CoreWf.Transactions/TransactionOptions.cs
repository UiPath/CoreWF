// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace System.Activities.Transactions
{
    public struct TransactionOptions
    {
        private TimeSpan _timeout;
        private IsolationLevel _isolationLevel;

        public TimeSpan Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        public IsolationLevel IsolationLevel
        {
            get { return _isolationLevel; }
            set { _isolationLevel = value; }
        }

        public override int GetHashCode() => base.GetHashCode();  // Don't have anything better to do.

        public override bool Equals(object obj) => obj is TransactionOptions && Equals((TransactionOptions)obj);

        private bool Equals(TransactionOptions other) =>
            _timeout == other._timeout &&
            _isolationLevel == other._isolationLevel;

        public static bool operator ==(TransactionOptions x, TransactionOptions y) => x.Equals(y);

        public static bool operator !=(TransactionOptions x, TransactionOptions y) => !x.Equals(y);
    }
}
