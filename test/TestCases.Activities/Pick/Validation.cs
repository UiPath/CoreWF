// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.Pick
{
    public class Validation
    {
        /// <summary>
        /// Pick with single branch without Trigger set.
        /// Trigger Not Set
        /// </summary>        
        [Fact]
        public void TriggerNotSet()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "Branch1",
                        Action = new TestWriteLine("Action1")
                        {
                            Message = "Action1",
                        },
                    },
                }
            };

            TestRuntime.ValidateInstantiationException(pick, String.Format(ErrorStrings.PickBranchRequiresTrigger, pick.Branches[0].DisplayName));
        }

        /// <summary>
        /// Pick with three branches, one of them don’t have Trigger set.
        /// One Trigger Not Set
        /// </summary>        
        [Fact]
        public void OneTriggerNotSet()
        {
            TestPick pick = new TestPick()
            {
                DisplayName = "PickActivity",
                Branches =
                {
                    new TestPickBranch()
                    {
                        DisplayName = "Branch1",
                        Trigger = new TestWriteLine("Trigger1")
                        {
                            Message = "Trigger1",
                        },
                        Action = new TestWriteLine("Action1")
                        {
                            Message = "Action1",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "Branch2",
                        Action = new TestWriteLine("Action2")
                        {
                            Message = "Action2",
                        },
                    },
                    new TestPickBranch()
                    {
                        DisplayName = "Branch3",
                        Trigger = new TestWriteLine("Trigger3")
                        {
                            Message = "Trigger3",
                        },
                        Action = new TestWriteLine("Action3")
                        {
                            Message = "Action3",
                        },
                    },
                }
            };

            TestRuntime.ValidateInstantiationException(pick, String.Format(ErrorStrings.PickBranchRequiresTrigger, pick.Branches[1].DisplayName));
        }

        ///// <summary>
        ///// Access the other branch's variable.
        ///// Access the other branch’s variable.
        ///// Access Other Variable
        ///// </summary>        
        //[Fact]
        //public void AccessOtherVariable()
        //{
        //    Variable<string> var1 = VariableHelper.CreateInitialized<string>("var1", "Variable1");
        //    Variable<string> var2 = VariableHelper.CreateInitialized<string>("var2", "Variable2");

        //    TestPick pick = new TestPick()
        //    {
        //        DisplayName = "PickActivity",
        //        Branches =
        //        {
        //            new TestPickBranch()
        //            {
        //                DisplayName = "Branch1",
        //                Variables =
        //                {
        //                    var1
        //                },
        //                Trigger = new TestWriteLine("Trigger1")
        //                {
        //                    Message = "Trigger1"
        //                },
        //                Action = new TestWriteLine("Action1")
        //                {
        //                    ExpectedOutcome = Outcome.UncaughtException(typeof(InvalidOperationException)),
        //                    HintMessage = "Variable2",
        //                    MessageActivity = new TestVisualBasicValue<string>("var2"),
        //                }
        //            },
        //            new TestPickBranch()
        //            {
        //                DisplayName = "Branch2",
        //                Variables =
        //                {
        //                    var2
        //                },
        //                Trigger = new TestBlockingActivity("Block")
        //                {
        //                    ExpectedOutcome = Outcome.None
        //                },
        //                Action = new TestWriteLine("Action2")
        //                {
        //                    Message = "Action2"
        //                }
        //            }
        //        }
        //    };

        //    TestRuntime.ValidateInstantiationException(
        //        pick,
        //       typeof(Microsoft.CoreWf.InvalidWorkflowException),
        //       "var2");
        //}

        /// <summary>
        /// Make sure Pick/PickBranch is sealed.
        /// Make sure Pick/PickBranch is sealed
        /// Sealed Test
        /// </summary>        
        [Fact]
        public void SealedTest()
        {
            Type pickType = typeof(Microsoft.CoreWf.Statements.Pick);
            Type pickBranchType = typeof(Microsoft.CoreWf.Statements.PickBranch);

            if (!pickType.GetTypeInfo().IsSealed)
            {
                throw new Exception("Microsoft.CoreWf.Statements.Pick should be sealed.");
            }

            if (!pickBranchType.GetTypeInfo().IsSealed)
            {
                throw new Exception("Microsoft.CoreWf.Statements.PickBranch should be sealed.");
            }
        }
    }
}
