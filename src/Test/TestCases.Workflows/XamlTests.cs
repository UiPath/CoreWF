using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using Xunit;

namespace TestCases.Workflows
{
    using IStringDictionary = IDictionary<string, object>;
    using StringDictionary = Dictionary<string, object>;

    public abstract class XamlTestsBase
    {
        protected abstract bool CompileExpressions { get; }
        protected IStringDictionary InvokeWorkflow(string xamlString, IStringDictionary inputs = null)
        {
            var activity = ActivityXamlServices.Load(new StringReader(xamlString), new ActivityXamlServicesSettings { CompileExpressions = CompileExpressions });
            return WorkflowInvoker.Invoke(activity, inputs ?? new StringDictionary());
        }
        public static IEnumerable<object[]> XamlNoInputs { get; } = new[]
        {
            new object[] { @"
                <Activity x:Class='WFTemplate'
                            xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                            xmlns:s='clr-namespace:System;assembly=mscorlib'
                            xmlns:s1='clr-namespace:System;assembly=System'
                            xmlns:sa='clr-namespace:System.Activities;assembly=System.Activities'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                </Activity>" },
            new object[] { @"
                <Activity x:Class='WFTemplate'
                            xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <Sequence>
                        <WriteLine Text='HelloWorld' />
                    </Sequence>
                </Activity>" },
            new object[] { @"
                <Activity
                            xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <Sequence>
                        <Sequence.Variables>
                            <Variable x:TypeArguments='x:String' Default='My variable text' Name='MyVar' />
                        </Sequence.Variables>
                        <WriteLine Text='[MyVar]' />
                    </Sequence>
                </Activity>" },
            new object[] { @"
                <Activity x:Class='WFTemplate'
                            xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <WriteLine Text='HelloWorld' />
                </Activity>" },
            new object[] { @"
                <Activity
                            xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <Sequence>
                        <Sequence.Variables>
                            <Variable x:TypeArguments='x:String' Default='My variable text' Name='text' />
                        </Sequence.Variables>
                        <WriteLine Text='[text]' />
                    </Sequence>
                </Activity>" },
        };

        [Theory]
        [MemberData(nameof(XamlNoInputs))]
        public void XamlWorkflowNoInputs(string xamlString) => InvokeWorkflow(xamlString);

        public static IEnumerable<object[]> XamlWithInputs { get; } = new[]
        {
            new object[] { @"
                <Activity x:Class='WFTemplate'
                            xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                            xmlns:s='clr-namespace:System;assembly=mscorlib'
                            xmlns:s1='clr-namespace:System;assembly=System'
                            xmlns:sa='clr-namespace:System.Activities;assembly=System.Activities'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <x:Members>
                        <x:Property Name='myInput' Type='InArgument(x:String)' />
                    </x:Members>
                    <WriteLine Text='[myInput]' />
                </Activity>" },
            new object[] { @"
                <Activity x:Class='WFTemplate'
                            xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                            xmlns:s='clr-namespace:System;assembly=mscorlib'
                            xmlns:s1='clr-namespace:System;assembly=System'
                            xmlns:sa='clr-namespace:System.Activities;assembly=System.Activities'
                            xmlns:hw='clr-namespace:TestCases.Workflows;assembly=TestCases.Workflows'
                            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                    <x:Members>
                        <x:Property Name='myInput' Type='InArgument(x:String)' />
                    </x:Members>
                    <hw:HelloWorldConsole Text='[myInput]' />
                </Activity>" },
        };

        [Theory]
        [MemberData(nameof(XamlWithInputs))]
        public void XamlWorkflowWithInputs(string xamlString) => InvokeWorkflow(xamlString, new StringDictionary { ["myInput"] = "HelloWorld" });

        [Fact]
        public void XamlWorkflowWithInputsOutputs()
        {
            var xamlString = @"
            <Activity x:Class='WFTemplate'
                      xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                      xmlns:s='clr-namespace:System;assembly=mscorlib'
                      xmlns:s1='clr-namespace:System;assembly=System'
                      xmlns:sa='clr-namespace:System.Activities;assembly=System.Activities'
                      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                <x:Members>
                    <x:Property Name='myOutput' Type='OutArgument(x:Int32)' />
                    <x:Property Name='myInput' Type='InArgument(x:Int32)' />
                </x:Members>
                <Assign>
                    <Assign.To>
                        <OutArgument x:TypeArguments='x:Int32'>[myOutput]</OutArgument>
                    </Assign.To>
                    <Assign.Value>
                        <InArgument x:TypeArguments='x:Int32'>[myInput]</InArgument>
                    </Assign.Value>
                </Assign>
            </Activity>";
            var outputs = InvokeWorkflow(xamlString, new StringDictionary { ["myInput"] = 1 });
            outputs.Count.ShouldBe(1);
            outputs.ContainsKey("myOutput").ShouldBeTrue();
            outputs["myOutput"].ShouldBe(1);
        }

        [Fact]
        public void XamlWorkflowWithInputObject()
        {
            var xamlString = @"
                    <Activity x:Class='WFTemplate'
                              xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                              xmlns:s='clr-namespace:System;assembly=mscorlib'
                              xmlns:s1='clr-namespace:System;assembly=System'
                              xmlns:sa='clr-namespace:System.Activities;assembly=System.Activities'
                              xmlns:hw='clr-namespace:TestCases.Workflows;assembly=TestCases.Workflows'
                              xmlns:sco='clr-namespace:System.Collections.ObjectModel;assembly=mscorlib'
                              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                        <x:Members>
                            <x:Property Name='myInput' Type='InArgument(hw:PersonToGreet)' />
                        </x:Members>
                        <TextExpression.ReferencesForImplementation>
                            <sco:Collection x:TypeArguments='AssemblyReference'>
                              <AssemblyReference>TestCases.Workflows</AssemblyReference>
                            </sco:Collection>
                        </TextExpression.ReferencesForImplementation>
                        <hw:ActivityWithObjectArgument Input='[myInput]' />
                    </Activity>";

            var inputs = new StringDictionary { ["myInput"] = new PersonToGreet { FirstName = "Jane", LastName = "Doe" } };
            var result = InvokeWorkflow(xamlString, inputs);
        }
    }
    public class JustInTimeXamlTests : XamlTestsBase
    {
        protected override bool CompileExpressions => false;
        [Fact]
        public void Should_infer_type()
        {
            var empty = Array.Empty<string>();
            var text = "\"test\"";
            var value = VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(null, text, empty, empty, null, out _, out _, out _);
            ((VisualBasicValue<string>)value).ExpressionText.ShouldBe(text);
            value = VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(null, text, empty, empty, null, out _, out _, out _);
            ((VisualBasicValue<string>)value).ExpressionText.ShouldBe(text);
        }
    }
    public class AheadOfTimeXamlTests : XamlTestsBase
    {
        protected override bool CompileExpressions => true;

        const string CSharpExpressions = @"
                <Activity x:Class='WFTemplate'
                          xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                          xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                          xmlns:mca='clr-namespace:Microsoft.CSharp.Activities;assembly=System.Activities'>
                    <Sequence>
                        <WriteLine>
                          <InArgument x:TypeArguments='x:String'>
                            <mca:CSharpValue x:TypeArguments='x:String'>""constant""</mca:CSharpValue>
                          </InArgument>
                        </WriteLine>
                    </Sequence>
                </Activity>";
        [Fact]
        public void CompileExpressionsDefault() => InvokeWorkflow(CSharpExpressions);
        [Fact]
        public void CompileExpressionsWithCompiler() =>
            new Action(()=>ActivityXamlServices.Load(new StringReader(CSharpExpressions), 
                new ActivityXamlServicesSettings { CSharpCompiler = new CSharpCompiler() })).ShouldThrow<NotImplementedException>();
        [Fact]
        public void CSharpCompileError()
        {
            var xaml = @"
                <Activity x:Class='WFTemplate'
                          xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                          xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                          xmlns:mca='clr-namespace:Microsoft.CSharp.Activities;assembly=System.Activities'>
                    <Sequence>
                        <WriteLine>
                          <InArgument x:TypeArguments='x:String'>
                            <mca:CSharpValue x:TypeArguments='x:String'>constant</mca:CSharpValue>
                          </InArgument>
                        </WriteLine>
                    </Sequence>
                </Activity>";
            new Action(() => InvokeWorkflow(xaml)).ShouldThrow<InvalidOperationException>().Data.Values.Cast<string>()
                .ShouldAllBe(error=>error.Contains("error CS0103: The name 'constant' does not exist in the current context"));
        }
        [Fact]
        public void CSharpInputOutput()
        {
            var xamlString = @"
                <Activity x:Class='WFTemplate'
                          xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                          xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                          xmlns:mca='clr-namespace:Microsoft.CSharp.Activities;assembly=System.Activities'>
                    <x:Members>
                        <x:Property Name='myOutput' Type='OutArgument(x:Int32)' />
                        <x:Property Name='myInput' Type='InArgument(x:Int32)' />
                    </x:Members>
                    <Sequence>
                        <Assign>
                          <Assign.To>
                            <OutArgument x:TypeArguments='x:Int32'>
                              <mca:CSharpReference x:TypeArguments='x:Int32'>myOutput</mca:CSharpReference>
                            </OutArgument>
                          </Assign.To>
                          <Assign.Value>
                            <InArgument x:TypeArguments='x:Int32'>
                              <mca:CSharpValue x:TypeArguments='x:Int32'>(myInput+myInput)/2</mca:CSharpValue>
                            </InArgument>
                          </Assign.Value>
                        </Assign>
                        <WriteLine>
                          <InArgument x:TypeArguments='x:String'>
                            <mca:CSharpValue x:TypeArguments='x:String'>myOutput.ToString()</mca:CSharpValue>
                          </InArgument>
                        </WriteLine>
                    </Sequence>
                </Activity>";
            var inputs = new StringDictionary { ["myInput"] = 5 };
            var outputs = InvokeWorkflow(xamlString, inputs);
            outputs["myOutput"].ShouldBe(5);
        }
        class CSharpCompiler : AheadOfTimeCompiler
        {
            public override TextExpressionCompilerResults Compile(ClassToCompile classToCompile) => throw new NotImplementedException();
        }
    }
}