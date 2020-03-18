// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities;
using System.Activities.Statements;

namespace ImperativeTestCases
{
    sealed class TransactionScopeTest : Activity
    {
        public TransactionScopeTest()
        {
            this.Implementation = () => new Sequence
            {
                Activities =
                {
                    new WriteLine { Text = "    Begin TransactionScopeTest" },

                    new TransactionScope
                    {
                        Body = new Sequence
                        {
                            Activities =
                            {
                                new WriteLine { Text = "    Begin TransactionScopeTest TransactionScope" },
                                new PrintTransactionId(),
                                new WriteLine { Text = "    End TransactionScopeTest TransactionScope" },
                            },
                        },
                    },

                    new WriteLine { Text = "    End TransactionScopeTest" },
                }
            };
        }
    }
}
