// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;

namespace ImperativeTestCases
{
    sealed class PrintTransactionId : NativeActivity
    {
        protected override void Execute(NativeActivityContext context)
        {
            //Access to the current transaction in Workflow is through the GetCurrentTransaction method on a RuntimeTransactionHandle
            RuntimeTransactionHandle rth = new RuntimeTransactionHandle();
            rth = context.Properties.Find(rth.ExecutionPropertyName) as RuntimeTransactionHandle;
            Console.WriteLine("    TransactionID: " + rth.GetCurrentTransaction(context).TransactionInformation.LocalIdentifier.ToString());
        }
    }
}
