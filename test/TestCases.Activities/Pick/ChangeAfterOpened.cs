// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.Pick
{
    public class ChangeAfterOpened
    {
        /// <summary>
        /// Add Branch after opened.
        /// Add Branch After Opened
        /// </summary>        
        [Fact]
        public void AddBranchAfterOpened()
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
                }
            };

            VerifyLocked(pick,
            delegate
            {
                pick.Branches.Add(new TestPickBranch());
            });
        }

        /// <summary>
        /// Change the Trigger after opened
        /// Change Trigger After Opened
        /// </summary>        
        [Fact]
        public void ChangeTriggerAfterOpened()
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
                }
            };

            VerifyLocked(pick,
            delegate
            {
                pick.Branches[0].Trigger = new TestWriteLine();
            });
        }

        /// <summary>
        /// Change the Action after opened
        /// Change Action After Opened
        /// </summary>        
        [Fact]
        public void ChangeActionAfterOpened()
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
                }
            };

            VerifyLocked(pick,
            delegate
            {
                pick.Branches[0].Action = new TestWriteLine();
            });
        }

        /// <summary>
        /// Add variable to PickBranch after opened.
        /// Add Variable After Opened
        /// </summary>        
        [Fact]
        public void AddVariableAfterOpened()
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
                }
            };

            VerifyLocked(pick,
            delegate
            {
                pick.Branches[0].Variables.Add(VariableHelper.Create<string>("var1"));
            });
        }

        private static void VerifyLocked(TestActivity activity, ExceptionHelpers.MethodDelegate tryCode)
        {
            //No excepion is expected.
            new WorkflowApplication(activity.ProductActivity);
            tryCode();
            new WorkflowApplication(activity.ProductActivity);
        }
    }
}
