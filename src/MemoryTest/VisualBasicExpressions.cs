using System;
using Xunit;

#if NET6_0_OR_GREATER
using System.Activities; // from CoreWF
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Microsoft.VisualBasic.Activities;
using Test.Common.TestObjects.Activities.Tracing;
#endif

#if NET48
using System;
using System.Linq;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;               // Literal<T>, ArgumentReference<T>
using Microsoft.VisualBasic.Activities;            // VisualBasic*, VisualBasicSettings
#endif

namespace MemoryTest
{
    public class VisualBasicExpressions
    {
#if NET6_0_OR_GREATER
        // 1) VB on RHS, WF variable on LHS (string chain)
        [Fact]
        public void MultipleAssign_UsingVBValueActivity()
        {
            int n = 100;
            var seq = new TestSequence();

            var vb = new VisualBasicSettings();
            vb.ImportReferences.Add(new VisualBasicImportReference
            {
                Assembly = typeof(string).Assembly.GetName().Name,
                Import = "System"
            });
            VisualBasic.SetSettings(seq, vb);

            var vars = new Variable<string>[n];

            for (int i = 0; i < n; i++)
            {
                var v = VariableHelper.CreateInitialized<string>($"var{i}", $"I'm variable {i}");
                vars[i] = v;
                seq.Variables.Add(v);

                string vbText =
                    (i == 0)
                    ? "\"I'm variable 0\""
                    : $"var{i - 1} & \" -> I'm variable {i}\"";  // VB uses & for string concat

                seq.Activities.Add(new TestAssign<string>
                {
                    // LHS = WF variable
                    ToVariable = v,
                    // RHS = VB value
                    ValueActivity = new TestVBValue<string>(vbText)
                });
            }

            seq.Activities.Add(new TestDelay
            {
                Duration = TimeSpan.FromSeconds(30),
                DisplayName = "Delay"
            });

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        // 2) VB on RHS, VB on LHS (string chain)
        [Fact]
        public void MultipleAssign_UsingVBReferenceOnLeft()
        {
            int n = 100;
            var seq = new TestSequence();

            var vb = new VisualBasicSettings();
            vb.ImportReferences.Add(new VisualBasicImportReference
            {
                Assembly = typeof(string).Assembly.GetName().Name,
                Import = "System"
            });
            VisualBasic.SetSettings(seq, vb);

            for (int i = 0; i < n; i++)
            {
                // Still create initialized WF variables for each name, so VB refs can bind.
                var v = VariableHelper.CreateInitialized<string>($"var{i}", $"I'm variable {i}");
                seq.Variables.Add(v);

                string vbText =
                    (i == 0)
                    ? "\"I'm variable 0\""
                    : $"var{i - 1} & \" -> I'm variable {i}\"";

                seq.Activities.Add(new TestAssign<string>
                {
                    // LHS = VB reference (e.g., "var12")
                    ToLocation = new TestVBReference<string>($"var{i}"),
                    // RHS = VB value
                    ValueActivity = new TestVBValue<string>(vbText)
                });
            }

            seq.Activities.Add(new TestDelay
            {
                Duration = TimeSpan.FromSeconds(30),
                DisplayName = "Delay"
            });

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        // 3) VB on RHS, WF variable on LHS (int math chain)
        [Fact]
        public void MultipleAssign_UsingVBValueActivity_IntMath()
        {
            int n = 100;
            var seq = new TestSequence();

            var vb = new VisualBasicSettings();
            vb.ImportReferences.Add(new VisualBasicImportReference
            {
                Assembly = typeof(int).Assembly.GetName().Name,
                Import = "System"
            });
            VisualBasic.SetSettings(seq, vb);

            for (int i = 0; i < n; i++)
            {
                var v = VariableHelper.CreateInitialized<int>($"var{i}", 0);
                seq.Variables.Add(v);

                string vbText = (i == 0) ? "0" : $"var{i - 1} + {i}";

                seq.Activities.Add(new TestAssign<int>
                {
                    ToVariable = v,                                      // LHS = WF variable
                    ValueActivity = new TestVBValue<int>(vbText)         // RHS = VB value
                });
            }

            seq.Activities.Add(new TestDelay
            {
                Duration = TimeSpan.FromSeconds(30),
                DisplayName = "Delay"
            });

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        // Helpers for CoreWF test harness
        public sealed class TestVBValue<T> : TestActivity
        {
            public TestVBValue(string expressionText)
            {
                this.ProductActivity = new VisualBasicValue<T>(expressionText);
                ExpectedOutcome = Outcome.None;
            }
        }

        public sealed class TestVBReference<T> : TestActivity
        {
            public TestVBReference(string expressionText)
            {
                this.ProductActivity = new VisualBasicReference<T>(expressionText);
                ExpectedOutcome = Outcome.None;
            }
        }
#else

        // 1) VB on RHS, WF variable on LHS (string chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingVBValueActivity_net48()
        {
            const int n = 100;

            var wf = new DynamicActivity<string>
            {
                DisplayName = "MultipleAssign_VBValue_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();

                    var vb = new VisualBasicSettings();
                    vb.ImportReferences.Add(new VisualBasicImportReference
                    {
                        Assembly = typeof(string).Assembly.GetName().Name,
                        Import = "System"
                    });
                    VisualBasic.SetSettings(seq, vb);

                    var vars = new Variable<string>[n];

                    for (int i = 0; i < n; i++)
                    {
                        var v = new Variable<string>($"var{i}")
                        {
                            Default = new Literal<string>($"I'm variable {i}")
                        };
                        vars[i] = v;
                        seq.Variables.Add(v);

                        string vbText = (i == 0)
                            ? "\"I'm variable 0\""
                            : $"var{i - 1} & \" -> I'm variable {i}\"";

                        seq.Activities.Add(new Assign<string>
                        {
                            // LHS = WF variable
                            To = new OutArgument<string>(v),
                            // RHS = VB value
                            Value = new InArgument<string>(new VisualBasicValue<string>(vbText))
                        });
                    }

                    seq.Activities.Add(new Delay
                    {
                        Duration = TimeSpan.FromSeconds(30),
                        DisplayName = "Delay"
                    });

                    // Result <- var{n-1}
                    seq.Activities.Add(new Assign<string>
                    {
                        To = new OutArgument<string>(new ArgumentReference<string>("Result")),
                        Value = new InArgument<string>(new VisualBasicValue<string>($"var{n - 1}"))
                    });

                    return seq;
                }
            };

            string actual = WorkflowInvoker.Invoke(wf);
            string expected = string.Join(" -> ", Enumerable.Range(0, n).Select(i => $"I'm variable {i}"));
            Assert.Equal(expected, actual);
        }

        // 2) VB on RHS, VB on LHS (string chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingVBReferenceOnLeft_net48()
        {
            const int n = 100;

            var wf = new DynamicActivity<string>
            {
                DisplayName = "MultipleAssign_VBReference_OnLeft_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();

                    var vb = new VisualBasicSettings();
                    vb.ImportReferences.Add(new VisualBasicImportReference
                    {
                        Assembly = typeof(string).Assembly.GetName().Name,
                        Import = "System"
                    });
                    VisualBasic.SetSettings(seq, vb);

                    for (int i = 0; i < n; i++)
                    {
                        // Define the variables to be referenced by VB.
                        var v = new Variable<string>($"var{i}") { Default = new Literal<string>($"I'm variable {i}") };
                        seq.Variables.Add(v);

                        string vbText = (i == 0)
                            ? "\"I'm variable 0\""
                            : $"var{i - 1} & \" -> I'm variable {i}\"";

                        seq.Activities.Add(new Assign<string>
                        {
                            // LHS = VB reference instead of variable
                            To = new OutArgument<string>(new VisualBasicReference<string>($"var{i}")),
                            // RHS = VB value
                            Value = new InArgument<string>(new VisualBasicValue<string>(vbText))
                        });
                    }

                    seq.Activities.Add(new Delay
                    {
                        Duration = TimeSpan.FromSeconds(30),
                        DisplayName = "Delay"
                    });

                    seq.Activities.Add(new Assign<string>
                    {
                        To = new OutArgument<string>(new ArgumentReference<string>("Result")),
                        Value = new InArgument<string>(new VisualBasicValue<string>($"var{n - 1}"))
                    });

                    return seq;
                }
            };

            string actual = WorkflowInvoker.Invoke(wf);
            string expected = string.Join(" -> ", Enumerable.Range(0, n).Select(i => $"I'm variable {i}"));
            Assert.Equal(expected, actual);
        }

        // 3) VB on RHS, WF variable on LHS (int math chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingVBValueActivity_IntMath_net48()
        {
            const int n = 100;

            var wf = new DynamicActivity<int>
            {
                DisplayName = "MultipleAssign_VBValue_Int_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();

                    var vb = new VisualBasicSettings();
                    vb.ImportReferences.Add(new VisualBasicImportReference
                    {
                        Assembly = typeof(int).Assembly.GetName().Name,
                        Import = "System"
                    });
                    VisualBasic.SetSettings(seq, vb);

                    for (int i = 0; i < n; i++)
                    {
                        var v = new Variable<int>($"var{i}") { Default = new Literal<int>(0) };
                        seq.Variables.Add(v);

                        string vbText = (i == 0) ? "0" : $"var{i - 1} + {i}";

                        seq.Activities.Add(new Assign<int>
                        {
                            // LHS = WF variable
                            To = new OutArgument<int>(v),
                            // RHS = VB value (arithmetic)
                            Value = new InArgument<int>(new VisualBasicValue<int>(vbText))
                        });
                    }

                    seq.Activities.Add(new Delay
                    {
                        Duration = TimeSpan.FromSeconds(30),
                        DisplayName = "Delay"
                    });

                    // Result <- var{n-1}
                    seq.Activities.Add(new Assign<int>
                    {
                        To = new OutArgument<int>(new ArgumentReference<int>("Result")),
                        Value = new InArgument<int>(new VisualBasicValue<int>($"var{n - 1}"))
                    });

                    return seq;
                }
            };

            int actual = WorkflowInvoker.Invoke(wf);
            int expected = n * (n - 1) / 2; // 0 + 1 + ... + (n-1)
            Assert.Equal(expected, actual);
        }
#endif
    }
}
