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
    public class UnitTest1 : BaseMemoryTest
    {
#if NET6_0_OR_GREATER
        [Fact]
        public void MultipleAssign_UsingVBValueActivity()
        {
            var seq = new TestSequence();

            // Recommended: attach VB settings to the root
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

                // Build VB expression text (remember to quote string literals in VB)
                string vbText =
                    (i == 0)
                    ? "\"I'm variable 0\""
                    : $"var{i - 1} & \" -> I'm variable {i}\"";  // VB uses & for string concat

                seq.Activities.Add(new TestAssign<string>
                {
                    // EITHER keep using a WF variable on the left:
                    ToVariable = v,

                    // OR, if you prefer VB on the left as well:
                    // ToLocation = new TestVBReference<string>($"var{i}"),

                    // ✅ Supply the VB expression via ValueActivity
                    ValueActivity = new TestVBValue<string>(vbText)
                });
            }

            seq.Activities.Add(new TestDelay
            {
                Duration = TimeSpan.FromSeconds(30),
                DisplayName = "Delay"
            });

            Validate(seq);
        }

        public sealed class TestVBValue<T> : TestActivity
        {
            public TestVBValue(string expressionText)
            {
                // RHS (r-value) VB expression
                this.ProductActivity = new VisualBasicValue<T>(expressionText);
                ExpectedOutcome = Outcome.None;
            }
        }

        public sealed class TestVBReference<T> : TestActivity
        {
            public TestVBReference(string expressionText)
            {
                // LHS (l-value) VB reference, if you ever want the left side in VB too
                this.ProductActivity = new VisualBasicReference<T>(expressionText);
                ExpectedOutcome = Outcome.None;
            }
        }
#else

        [Fact]
        public void MultipleAssign_UsingVBValueActivity_net48()
        {
            // Root is an Activity<string> so we can assert the final value easily.
            var wf = new DynamicActivity<string>
            {
                DisplayName = "MultipleAssign_VBValue_SystemActivities",
                Implementation = () =>
                {
                    var seq = new Sequence();

                    // Attach VB settings to the root (so VB expressions can resolve imports).
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
                        // Create the WF variable and give it an initial default.
                        var v = new Variable<string>($"var{i}")
                        {
                            // Default expects an Activity<T>; Literal<T> is perfect here.
                            Default = new Literal<string>($"I'm variable {i}")
                        };
                        vars[i] = v;
                        seq.Variables.Add(v);

                        // Build the VB expression text (remember to quote VB string literals).
                        string vbText = (i == 0)
                            ? "\"I'm variable 0\""
                            : $"var{i - 1} & \" -> I'm variable {i}\"";

                        // Assign using VB on the RHS; for the LHS we reference the variable by name via VB too.
                        seq.Activities.Add(new Assign<string>
                        {
                            To = new OutArgument<string>(new VisualBasicReference<string>($"var{i}")),
                            Value = new InArgument<string>(new VisualBasicValue<string>(vbText))
                        });
                    }

                    // Keep the delay tiny so the test is fast; change to 60s if you need the original behavior.
                    seq.Activities.Add(new Delay
                    {
                        Duration = TimeSpan.FromSeconds(30),
                        DisplayName = "Delay"
                    });

                    // Surface the final string as the activity's Result so the unit test can assert it.
                    seq.Activities.Add(new Assign<string>
                    {
                        // ArgumentReference targets the root activity's "Result" argument by name.
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