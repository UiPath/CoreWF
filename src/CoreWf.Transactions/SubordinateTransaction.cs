// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.


namespace System.Activities.Transactions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229", Justification = "Serialization not yet supported and will be done using DistributedTransaction")]
    public sealed class SubordinateTransaction : Transaction
    {
        // Create a transaction with the given settings
        //
        public SubordinateTransaction(IsolationLevel isoLevel, ISimpleTransactionSuperior superior) : base(isoLevel, superior)
        {
        }
    }
}
