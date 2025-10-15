using System;
using Xunit;
using System.Linq.Expressions;

#if NET6_0_OR_GREATER
using System.Activities; // CoreWF
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Activities.Tracing;
using System.Activities.Expressions;              // LambdaValue<T>, LambdaReference<T>
#endif

#if NET48
using System;
using System.Linq;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;              // Literal<T>, ArgumentReference<T>, Lambda*
#endif

namespace MemoryTest
{
    public class LambdaExpressionTests
    {
#if NET6_0_OR_GREATER
        // 1) Lambda on RHS, WF variable on LHS (string chain)
        [Fact]
        public void MultipleAssign_UsingLambdaValue()
        {
            int n = 100;
            var seq = new TestSequence();
            var vars = new Variable<string>[n];

            for (int i = 0; i < n; i++)
            {
                int idx = i; // avoid closure capture issue
                var v = VariableHelper.CreateInitialized<string>($"var{idx}", $"I'm variable {idx}");
                vars[idx] = v;
                seq.Variables.Add(v);

                // IMPORTANT: build different lambdas so the expression tree for idx==0
                // does not contain a "vars[idx-1]" access (WF validates eagerly).
                if (idx == 0)
                {
                    seq.Activities.Add(new TestAssign<string>
                    {
                        ToVariable = v,
                        ValueActivity = new TestLambdaValue<string>(ctx => "I'm variable 0")
                    });
                }
                else
                {
                    seq.Activities.Add(new TestAssign<string>
                    {
                        ToVariable = v,
                        ValueActivity = new TestLambdaValue<string>(ctx =>
                            vars[idx - 1].Get(ctx) + $" -> I'm variable {idx}")
                    });
                }
            }

            seq.Activities.Add(new TestDelay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        // 2) Lambda reference on LHS (string chain)
        [Fact]
        public void MultipleAssign_UsingLambdaReferenceOnLeft()
        {
            int n = 100;
            var seq = new TestSequence();
            var vars = new Variable<string>[n];

            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var v = VariableHelper.CreateInitialized<string>($"var{idx}", $"I'm variable {idx}");
                vars[idx] = v;
                seq.Variables.Add(v);

                if (idx == 0)
                {
                    seq.Activities.Add(new TestAssign<string>
                    {
                        ToLocation = new TestLambdaReference<string>(ctx => vars[idx].Get(ctx)),
                        ValueActivity = new TestLambdaValue<string>(ctx => "I'm variable 0")
                    });
                }
                else
                {
                    seq.Activities.Add(new TestAssign<string>
                    {
                        ToLocation = new TestLambdaReference<string>(ctx => vars[idx].Get(ctx)),
                        ValueActivity = new TestLambdaValue<string>(ctx =>
                            vars[idx - 1].Get(ctx) + $" -> I'm variable {idx}")
                    });
                }
            }

            seq.Activities.Add(new TestDelay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        // 3) Lambda on RHS, WF variable on LHS (int math chain)
        [Fact]
        public void MultipleAssign_UsingLambdaValue_IntMath()
        {
            int n = 100;
            var seq = new TestSequence();
            var vars = new Variable<int>[n];

            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var v = VariableHelper.CreateInitialized<int>($"var{idx}", 0);
                vars[idx] = v;
                seq.Variables.Add(v);

                if (idx == 0)
                {
                    seq.Activities.Add(new TestAssign<int>
                    {
                        ToVariable = v,
                        ValueActivity = new TestLambdaValue<int>(ctx => 0)
                    });
                }
                else
                {
                    seq.Activities.Add(new TestAssign<int>
                    {
                        ToVariable = v,
                        ValueActivity = new TestLambdaValue<int>(ctx =>
                            vars[idx - 1].Get(ctx) + idx)
                    });
                }
            }

            seq.Activities.Add(new TestDelay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        // --- helpers (CoreWF harness wrappers for lambda) ---
        public sealed class TestLambdaValue<T> : TestActivity
        {
            public TestLambdaValue(Expression<Func<ActivityContext, T>> expression)
            {
                this.ProductActivity = new LambdaValue<T>(expression);
                ExpectedOutcome = Outcome.None;
            }
        }

        public sealed class TestLambdaReference<T> : TestActivity
        {
            // Expression returns T (an l-value), not Location<T>
            public TestLambdaReference(Expression<Func<ActivityContext, T>> lvalueExpression)
            {
                this.ProductActivity = new LambdaReference<T>(lvalueExpression);
                ExpectedOutcome = Outcome.None;
            }
        }

#else // NET48

        // 1) Lambda on RHS, WF variable on LHS (string chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingLambdaValue_net48()
        {
            const int n = 100;

            var wf = new DynamicActivity<string>
            {
                DisplayName = "MultipleAssign_LambdaValue_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();
                    var vars = new Variable<string>[n];

                    for (int i = 0; i < n; i++)
                    {
                        int idx = i;
                        var v = new Variable<string>($"var{idx}") { Default = new Literal<string>($"I'm variable {idx}") };
                        vars[idx] = v;
                        seq.Variables.Add(v);

                        if (idx == 0)
                        {
                            seq.Activities.Add(new Assign<string>
                            {
                                To = new OutArgument<string>(v),
                                Value = new InArgument<string>(ctx => "I'm variable 0")
                            });
                        }
                        else
                        {
                            seq.Activities.Add(new Assign<string>
                            {
                                To = new OutArgument<string>(v),
                                Value = new InArgument<string>(ctx => vars[idx - 1].Get(ctx) + $" -> I'm variable {idx}")
                            });
                        }
                    }

                    seq.Activities.Add(new Delay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

                    seq.Activities.Add(new Assign<string>
                    {
                        To = new OutArgument<string>(new ArgumentReference<string>("Result")),
                        Value = new InArgument<string>(ctx => vars[n - 1].Get(ctx))
                    });

                    return seq;
                }
            };

            string actual = WorkflowInvoker.Invoke(wf);
            string expected = string.Join(" -> ", Enumerable.Range(0, n).Select(i => $"I'm variable {i}"));
            Assert.Equal(expected, actual);
        }

        // 2) Lambda reference on LHS (string chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingLambdaReferenceOnLeft_net48()
        {
            const int n = 100;

            var wf = new DynamicActivity<string>
            {
                DisplayName = "MultipleAssign_LambdaReference_OnLeft_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();
                    var vars = new Variable<string>[n];

                    for (int i = 0; i < n; i++)
                    {
                        int idx = i;
                        var v = new Variable<string>($"var{idx}") { Default = new Literal<string>($"I'm variable {idx}") };
                        vars[idx] = v;
                        seq.Variables.Add(v);

                        if (idx == 0)
                        {
                            seq.Activities.Add(new Assign<string>
                            {
                                To = new OutArgument<string>(ctx => vars[idx].Get(ctx)),
                                Value = new InArgument<string>(ctx => "I'm variable 0")
                            });
                        }
                        else
                        {
                            seq.Activities.Add(new Assign<string>
                            {
                                To = new OutArgument<string>(ctx => vars[idx].Get(ctx)),
                                Value = new InArgument<string>(ctx => vars[idx - 1].Get(ctx) + $" -> I'm variable {idx}")
                            });
                        }
                    }

                    seq.Activities.Add(new Delay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

                    seq.Activities.Add(new Assign<string>
                    {
                        To = new OutArgument<string>(new ArgumentReference<string>("Result")),
                        Value = new InArgument<string>(ctx => vars[n - 1].Get(ctx))
                    });

                    return seq;
                }
            };

            string actual = WorkflowInvoker.Invoke(wf);
            string expected = string.Join(" -> ", Enumerable.Range(0, n).Select(i => $"I'm variable {i}"));
            Assert.Equal(expected, actual);
        }

        // 3) Lambda on RHS, WF variable on LHS (int math chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingLambdaValue_IntMath_net48()
        {
            const int n = 100;

            var wf = new DynamicActivity<int>
            {
                DisplayName = "MultipleAssign_LambdaValue_Int_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();
                    var vars = new Variable<int>[n];

                    for (int i = 0; i < n; i++)
                    {
                        int idx = i;
                        var v = new Variable<int>($"var{idx}") { Default = new Literal<int>(0) };
                        vars[idx] = v;
                        seq.Variables.Add(v);

                        if (idx == 0)
                        {
                            seq.Activities.Add(new Assign<int>
                            {
                                To = new OutArgument<int>(v),
                                Value = new InArgument<int>(ctx => 0)
                            });
                        }
                        else
                        {
                            seq.Activities.Add(new Assign<int>
                            {
                                To = new OutArgument<int>(v),
                                Value = new InArgument<int>(ctx => vars[idx - 1].Get(ctx) + idx)
                            });
                        }
                    }

                    seq.Activities.Add(new Delay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

                    seq.Activities.Add(new Assign<int>
                    {
                        To = new OutArgument<int>(new ArgumentReference<int>("Result")),
                        Value = new InArgument<int>(ctx => vars[n - 1].Get(ctx))
                    });

                    return seq;
                }
            };

            int actual = WorkflowInvoker.Invoke(wf);
            int expected = n * (n - 1) / 2;
            Assert.Equal(expected, actual);
        }
#endif
    }
}
