//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

using System;
using CoreWf;

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
