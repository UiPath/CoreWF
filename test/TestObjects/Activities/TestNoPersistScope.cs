// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.Statements;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    // NonPersistingSequence isn't a publicly-exposed activity, but it
    // still shows up in the tracing for InvokePowerShell and Send/ReceiveReply, etc.
    // because they each use NonpersistingSequence in their Body,
    // so we have a test class to make the tracing appear in tests.
    public class TestMessagingNoPersistScope : TestSequence
    {
        public TestMessagingNoPersistScope()
            : this("MessagingNoPersistScope")
        {
        }

        public TestMessagingNoPersistScope(string DisplayName)
        {
            // NonPersistingSequence and Sequence aren't actually 
            // related, but structurally they're analogous.                                        
            this.ProductActivity = new Sequence();
            this.DisplayName = DisplayName;
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            TestSequence seq = new TestSequence("Sequence");

            foreach (TestActivity ta in this.Activities)
            {
                seq.Activities.Add(ta);
            }

            CurrentOutcome = seq.GetTrace(traceGroup);
        }
    }
}



