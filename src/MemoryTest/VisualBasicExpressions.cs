using System;
using Xunit;


#if NET6_0_OR_GREATER
using static TestCases.Activities.Assignment;
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
    public class VisualBasicExpressions : BaseMemoryTest
    {
#if NET6_0_OR_GREATER
        // 1) VB on RHS, WF variable on LHS (string chain)
        [Fact]
        public void Windows_MultipleAssign_VBValue()
        {
            var seq = new TestSequence();

            var vb = new VisualBasicSettings();
            vb.ImportReferences.Add(new VisualBasicImportReference
            {
                Assembly = typeof(string).Assembly.GetName().Name,
                Import = "System"
            });
            VisualBasic.SetSettings(seq, vb);

            var vars = new Variable<string>[VariableAndArgumentCount];

            for (int i = 0; i < VariableAndArgumentCount; i++)
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
            seq = AddFinalActivities(seq, nameof(Windows_MultipleAssign_VBValue));

            Validate(seq);
        }

        // 2) VB on RHS, VB on LHS (string chain)
        [Fact]
        public void Windows_MultipleAssign_VBReference()
        {
            var seq = new TestSequence();

            var vb = new VisualBasicSettings();
            vb.ImportReferences.Add(new VisualBasicImportReference
            {
                Assembly = typeof(string).Assembly.GetName().Name,
                Import = "System"
            });
            VisualBasic.SetSettings(seq, vb);

            for (int i = 0; i < VariableAndArgumentCount; i++)
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

            seq = AddFinalActivities(seq, nameof(Windows_MultipleAssign_VBReference));

            Validate(seq);
        }

#else

        // 1) VB on RHS, WF variable on LHS (string chain) — asserts result
        [Fact]
        public void Legacy_multipleAssign_VbValue()
        {
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

                    var vars = new Variable<string>[VariableAndArgumentCount];

                    for (int i = 0; i < VariableAndArgumentCount; i++)
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

                    seq = AddFinalActivities(seq, nameof(Legacy_multipleAssign_VbValue));

                    // Result <- var{n-1}
                    seq.Activities.Add(new Assign<string>
                    {
                        To = new OutArgument<string>(new ArgumentReference<string>("Result")),
                        Value = new InArgument<string>(new VisualBasicValue<string>($"var{VariableAndArgumentCount - 1}"))
                    });

                    return seq;
                }
            };

            Validate(wf);
        }

        // 2) VB on RHS, VB on LHS (string chain) — asserts result
        [Fact]
        public void Legacy_multipleAssign_VbReference()
        {
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

                    for (int i = 0; i < VariableAndArgumentCount; i++)
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

                    seq = AddFinalActivities(seq, nameof(Legacy_multipleAssign_VbReference));

                    seq.Activities.Add(new Assign<string>
                    {
                        To = new OutArgument<string>(new ArgumentReference<string>("Result")),
                        Value = new InArgument<string>(new VisualBasicValue<string>($"var{VariableAndArgumentCount - 1}"))
                    });

                    return seq;
                }
            };

            Validate(wf);
        }
#endif
    }
}
