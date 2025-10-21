using System;
using Xunit;

using System.Activities.XamlIntegration;
using System.Activities.Expressions;

#if NET6_0_OR_GREATER
using System.Activities; // CoreWF
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Activities.Tracing;
using Microsoft.CSharp.Activities;               // CSharpValue<T>, CSharpReference<T>
#endif

#if NET48
using System;
using System.Linq;
using System.Activities;
using System.Activities.Statements;
using System.Activities.Expressions;              // Literal<T>, ArgumentReference<T>
using Microsoft.CSharp.Activities;               // CSharpValue<T>, CSharpReference<T>
#endif

namespace MemoryTest
{
    public class CSharpExpressionTests : BaseMemoryTest
    {
#if NET6_0_OR_GREATER
        // Helper to compile C# expressions for CoreWF TestSequence roots.
        private sealed class CompilableTestSequence : TestSequence
        {
            public void CompileCSharpExpressions()
            {
                var settings = new TextExpressionCompilerSettings
                {
                    Activity = Root,
                    Language = "C#",
                    ActivityName = "CompiledExpressionRoot",
                    ActivityNamespace = "MemoryTest.Compiled",
                    RootNamespace = null,
                    GenerateAsPartialClass = false,
                    AlwaysGenerateSource = true,
                    ForImplementation = true,
                    Compiler = new CSharpAotCompiler()
                };

                var results = new TextExpressionCompiler(settings).Compile();

                var compiledExpressionRootType = results.ResultType;

                var compiledExpressionRoot =
                    Activator.CreateInstance(compiledExpressionRootType, Root) as ICompiledExpressionRoot;
                CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(Root, compiledExpressionRoot);
            }
            public Activity Root => (Activity)this.ProductActivity;
        }

        // 1) C# on RHS, WF variable on LHS (string chain)
        [Fact]
        public void MultipleAssign_UsingCSharpValueActivity()
        {
            var seq = new CompilableTestSequence();

            var vars = new Variable<string>[VariableAndArgumentCount];

            for (int i = 0; i < VariableAndArgumentCount; i++)
            {
                var v = VariableHelper.CreateInitialized<string>($"var{i}", $"I'm variable {i}");
                vars[i] = v;
                seq.Variables.Add(v);

                string csText = (i == 0)
                    ? "\"I'm variable 0\""
                    : $"var{i - 1} + \" -> I'm variable {i}\"";

                seq.Activities.Add(new TestAssign<string>
                {
                    ToVariable = v,
                    ValueActivity = new TestCSValue<string>(csText)
                });
            }

            seq.Activities.Add(new TestDelay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

            // Compile C# expressions then run
            seq.CompileCSharpExpressions();
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        // 2) C# on RHS, C# reference on LHS (string chain)
        [Fact]
        public void MultipleAssign_UsingCSharpReferenceOnLeft()
        {
            var seq = new CompilableTestSequence();

            for (int i = 0; i < VariableAndArgumentCount; i++)
            {
                var v = VariableHelper.CreateInitialized<string>($"var{i}", $"I'm variable {i}");
                seq.Variables.Add(v);

                string csText = (i == 0)
                    ? "\"I'm variable 0\""
                    : $"var{i - 1} + \" -> I'm variable {i}\"";

                seq.Activities.Add(new TestAssign<string>
                {
                    ToLocation = new TestCSReference<string>($"var{i}"),
                    ValueActivity = new TestCSValue<string>(csText)
                });
            }

            seq.Activities.Add(new TestDelay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

            // Compile C# expressions then run
            seq.CompileCSharpExpressions();
            //TestRuntime.RunAndValidateWorkflow(seq);
            WorkflowInvoker.Invoke(seq.Root);
        }

        // 3) C# on RHS, WF variable on LHS (int math chain)
        [Fact]
        public void MultipleAssign_UsingCSharpValueActivity_IntMath()
        {
            var seq = new CompilableTestSequence();

            for (int i = 0; i < VariableAndArgumentCount; i++)
            {
                var v = VariableHelper.CreateInitialized<int>($"var{i}", 0);
                seq.Variables.Add(v);

                string csText = (i == 0) ? "0" : $"var{i - 1} + {i}";

                seq.Activities.Add(new TestAssign<int>
                {
                    ToVariable = v,
                    ValueActivity = new TestCSValue<int>(csText)
                });
            }

            seq.Activities.Add(new TestDelay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

            // Compile C# expressions then run
            seq.CompileCSharpExpressions();
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        // --- helpers (CoreWF harness wrappers) ---
        public sealed class TestCSValue<T> : TestActivity
        {
            public TestCSValue(string expressionText)
            {
                this.ProductActivity = new CSharpValue<T>(expressionText);
                ExpectedOutcome = Outcome.None;
            }
        }

        public sealed class TestCSReference<T> : TestActivity
        {
            public TestCSReference(string expressionText)
            {
                this.ProductActivity = new CSharpReference<T>(expressionText);
                ExpectedOutcome = Outcome.None;
            }
        }

#else // NET48

        // Compile C# text expressions for any Activity root (works with DynamicActivity<T>).
        private static void CompileCSharpExpressions(Activity root, string nameHint)
        {
            var settings = new TextExpressionCompilerSettings
            {
                Activity = root,
                Language = "C#",
                ActivityName = nameHint + "_CompiledExpressionRoot",
                ActivityNamespace = "MemoryTest.Compiled",
                RootNamespace = null,
                GenerateAsPartialClass = false,
                AlwaysGenerateSource = true,
                ForImplementation = true
            };

            var results = new TextExpressionCompiler(settings).Compile();
            if (results.HasErrors)
                throw new InvalidOperationException("C# expression compilation failed.");

            var compiled = (ICompiledExpressionRoot)Activator.CreateInstance(results.ResultType, new object[] { root });
            CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(root, compiled);
        }

        // 1) C# on RHS, WF variable on LHS (string chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingCSharpValueActivity_net48()
        {
            var wf = new DynamicActivity<string>
            {
                DisplayName = "MultipleAssign_CSharpValue_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();
                    var vars = new Variable<string>[VariableAndArgumentCount];

                    for (int i = 0; i < VariableAndArgumentCount; i++)
                    {
                        var v = new Variable<string>($"var{i}") { Default = new Literal<string>($"I'm variable {i}") };
                        vars[i] = v;
                        seq.Variables.Add(v);

                        string csText = (i == 0)
                            ? "\"I'm variable 0\""
                            : $"var{i - 1} + \" -> I'm variable {i}\"";

                        seq.Activities.Add(new Assign<string>
                        {
                            To = new OutArgument<string>(v),
                            Value = new InArgument<string>(new CSharpValue<string>(csText))
                        });
                    }

                    seq.Activities.Add(new Delay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

                    seq.Activities.Add(new Assign<string>
                    {
                        To = new OutArgument<string>(new ArgumentReference<string>("Result")),
                        Value = new InArgument<string>(new CSharpValue<string>($"var{VariableAndArgumentCount - 1}"))
                    });

                    return seq;
                }
            };

            // compile then run
            CompileCSharpExpressions(wf, "CSharpValue_StringChain");

            string actual = WorkflowInvoker.Invoke(wf);
            string expected = string.Join(" -> ", Enumerable.Range(0, VariableAndArgumentCount).Select(i => $"I'm variable {i}"));
            Assert.Equal(expected, actual);
        }

        // 2) C# on RHS, C# reference on LHS (string chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingCSharpReferenceOnLeft_net48()
        {
            var wf = new DynamicActivity<string>
            {
                DisplayName = "MultipleAssign_CSharpReference_OnLeft_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();

                    for (int i = 0; i < VariableAndArgumentCount; i++)
                    {
                        var v = new Variable<string>($"var{i}") { Default = new Literal<string>($"I'm variable {i}") };
                        seq.Variables.Add(v);

                        string csText = (i == 0)
                            ? "\"I'm variable 0\""
                            : $"var{i - 1} + \" -> I'm variable {i}\"";

                        seq.Activities.Add(new Assign<string>
                        {
                            To = new OutArgument<string>(new CSharpReference<string>($"var{i}")),
                            Value = new InArgument<string>(new CSharpValue<string>(csText))
                        });
                    }

                    seq.Activities.Add(new Delay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

                    seq.Activities.Add(new Assign<string>
                    {
                        To = new OutArgument<string>(new ArgumentReference<string>("Result")),
                        Value = new InArgument<string>(new CSharpValue<string>($"var{VariableAndArgumentCount - 1}"))
                    });

                    return seq;
                }
            };

            // compile then run
            CompileCSharpExpressions(wf, "CSharpReference_StringChain");

            string actual = WorkflowInvoker.Invoke(wf);
            string expected = string.Join(" -> ", Enumerable.Range(0, VariableAndArgumentCount).Select(i => $"I'm variable {i}"));
            Assert.Equal(expected, actual);
        }

        // 3) C# on RHS, WF variable on LHS (int math chain) — asserts result
        [Fact]
        public void MultipleAssign_UsingCSharpValueActivity_IntMath_net48()
        {
            var wf = new DynamicActivity<int>
            {
                DisplayName = "MultipleAssign_CSharpValue_Int_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();

                    for (int i = 0; i < VariableAndArgumentCount; i++)
                    {
                        var v = new Variable<int>($"var{i}") { Default = new Literal<int>(0) };
                        seq.Variables.Add(v);

                        string csText = (i == 0) ? "0" : $"var{i - 1} + {i}";

                        seq.Activities.Add(new Assign<int>
                        {
                            To = new OutArgument<int>(v),
                            Value = new InArgument<int>(new CSharpValue<int>(csText))
                        });
                    }

                    seq.Activities.Add(new Delay { Duration = TimeSpan.FromSeconds(30), DisplayName = "Delay" });

                    seq.Activities.Add(new Assign<int>
                    {
                        To = new OutArgument<int>(new ArgumentReference<int>("Result")),
                        Value = new InArgument<int>(new CSharpValue<int>($"var{VariableAndArgumentCount - 1}"))
                    });

                    return seq;
                }
            };

            // compile then run
            CompileCSharpExpressions(wf, "CSharpValue_IntChain");

            int actual = WorkflowInvoker.Invoke(wf);
            int expected = VariableAndArgumentCount * (VariableAndArgumentCount - 1) / 2;
            Assert.Equal(expected, actual);
        }
#endif
    }
}
