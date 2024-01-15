using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Activities;
using System.Activities.ExpressionParser;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
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
            var activity = Load(xamlString);
            return WorkflowInvoker.Invoke(activity, inputs ?? new StringDictionary());
        }
        protected Activity Load(string xamlString) =>
            ActivityXamlServices.Load(new StringReader(xamlString), new ActivityXamlServicesSettings { CompileExpressions = CompileExpressions });
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
        [Theory]
        [ClassData(typeof(VisualBasicInferTypeData))]
        public void VisualBasicShould_Infer_Type(string text, Type resultType, string[] namespaces, string[] assemblies)
        {
            VbCompile(text, resultType, namespaces, assemblies);
            // this one is cached
            VbCompile(text, resultType, namespaces, assemblies);
        }

        [Fact]
        public void VisualBasic_ChangeCompiler()
        {
            var empty = Array.Empty<string>();
            var activity = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("\"a\"")) };
            VisualBasicSettings.Default.CompilerFactory = _ => new ThrowingJitCompiler();
            // this one is cached
            new Action(() => WorkflowInspectionServices.CacheMetadata(activity)).ShouldThrow<NotImplementedException>();
            VisualBasicSettings.Default.CompilerFactory = references => new VbJitCompiler(references);
        }

        private class ThrowingJitCompiler : JustInTimeCompiler
        {
            public override LambdaExpression CompileExpression(ExpressionToCompile compilerRequest) => throw new NotImplementedException();
        }
        private static void VbCompile(string text, Type resultType, string[] namespaces, string[] assemblies)
        {
            var value = VisualBasicDesignerHelper.CreatePrecompiledVisualBasicValue(null, text, namespaces, assemblies, null, out var returnType, out var compileError, out var settings);
            Check(text, resultType, value, returnType, compileError, settings);
        }
        private static void CSharpCompile(string text, Type resultType, string[] namespaces, string[] assemblies)
        {
            var value = CSharpDesignerHelper.CreatePrecompiledValue(null, text, namespaces, assemblies, null, out var returnType, out var compileError, out var settings);
            Check(text, resultType, value, returnType, compileError, settings);
        }
        private static void Check(string text, Type resultType, Activity value, Type returnType, SourceExpressionException compileError, VisualBasicSettings settings)
        {
            ((ITextExpression)value).ExpressionText.ShouldBe(text);
            ((ActivityWithResult)value).ResultType.ShouldBe(resultType);
            returnType.ShouldBe(resultType);
            compileError.ShouldBeNull();
        }
        [Theory]
        [ClassData(typeof(CSharpInferTypeData))]
        public void CSharpShould_Infer_Type(string text, Type resultType, string[] namespaces, string[] assemblies)
        {
            CSharpCompile(text, resultType, namespaces, assemblies);
            // this one is cached
            CSharpCompile(text, resultType, namespaces, assemblies);
        }

        [Theory]
        [MemberData(nameof(GetCSharpTestData))]
        public async Task CS_CreatePrecompiledValueAsync(string expression, IEnumerable<string> namespaces, IEnumerable<string> assemblies, IEnumerable<VisualBasicImportReference> importReferences)
        {
            var result = await CSharpDesignerHelper.CreatePrecompiledValueAsync(null, expression, namespaces, assemblies, null);

            foreach (var reference in importReferences)
            {
                result.VisualBasicSettings.ImportReferences.ShouldContain(reference, $"Did not contain namespace {reference.Import} from assembly {reference.Assembly}");
            }
        }

        [Theory]
        [MemberData(nameof(GetVBTestData))]
        public async Task VB_CreatePrecompiledValueAsync(string expression, IEnumerable<string> namespaces, IEnumerable<string> assemblies, IEnumerable<VisualBasicImportReference> importReferences)
        {
            var result = await VisualBasicDesignerHelper.CreatePrecompiledValueAsync(null, expression, namespaces, assemblies, null);

            foreach (var reference in importReferences)
            {
                result.VisualBasicSettings.ImportReferences.ShouldContain(reference, $"Did not contain namespace {reference.Import} from assembly {reference.Assembly}");
            }
        }

        public static IEnumerable<object[]> GetCSharpTestData
        {
            get
            {
                yield return new object[] { "typeof(JsonConvert)", new[] { "Newtonsoft.Json" }, new[] { "Newtonsoft.Json" }, new[] { new VisualBasicImportReference { Assembly = "Newtonsoft.Json", Import = "Newtonsoft.json" } } };
                yield return new object[] { "new JToken[5]", new[] { "Newtonsoft.Json", "Newtonsoft.Json.Linq" }, new[] { "Newtonsoft.Json" }, new[] { new VisualBasicImportReference { Assembly = "Newtonsoft.Json", Import = "Newtonsoft.json" } } };
                yield return new object[] { "new Dictionary<Dictionary<JToken, string>, JToken>()", new[] { "System.Collections.Generic", "Newtonsoft.Json", "Newtonsoft.Json.Linq" }, new[] { "Newtonsoft.Json" }, new[] { new VisualBasicImportReference { Assembly = "Newtonsoft.Json", Import = "Newtonsoft.json" } } };
                yield return new object[] { "new ClassWithDictionaryProperty().TestProperty", new[] { "TestCases.Workflows" }, new[] { "System.Collections", "TestCases.Workflows", }, new[] { new VisualBasicImportReference { Assembly = "TestCases.Workflows", Import = "TestCases.Workflows" }, new VisualBasicImportReference { Assembly = "System.Private.CoreLib", Import = "System.Collections" } } };
            }
        }

        public static IEnumerable<object[]> GetVBTestData
        {
            get
            {
                yield return new object[] { "GetType(JsonConvert)", new[] { "Newtonsoft.Json" }, new[] { "Newtonsoft.Json" }, new[] { new VisualBasicImportReference { Assembly = "Newtonsoft.Json", Import = "Newtonsoft.json" } } };
                yield return new object[] { "new JToken(5){}", new[] { "Newtonsoft.Json", "Newtonsoft.Json.Linq" }, new[] { "Newtonsoft.Json" }, new[] { new VisualBasicImportReference { Assembly = "Newtonsoft.Json", Import = "Newtonsoft.json" } } };
                yield return new object[] { "new Dictionary(Of Dictionary(Of JToken, String), JToken)()", new[] { "System.Collections.Generic", "Newtonsoft.Json", "Newtonsoft.Json.Linq" }, new[] { "Newtonsoft.Json" }, new[] { new VisualBasicImportReference { Assembly = "Newtonsoft.Json", Import = "Newtonsoft.json" } } };
                yield return new object[] { "new ClassWithDictionaryProperty().TestProperty", new[] { "TestCases.Workflows" }, new[] { "System.Collections", "TestCases.Workflows", }, new[] { new VisualBasicImportReference { Assembly = "TestCases.Workflows", Import = "TestCases.Workflows" }, new VisualBasicImportReference { Assembly = "System.Private.CoreLib", Import = "System.Collections" } } };
            }
        }

        public class CSharpInferTypeData : TheoryData<string, Type, string[], string[]>
        {
            public CSharpInferTypeData()
            {
                var empty = Array.Empty<string>();
                Add("\"abc\"", typeof(string), empty, empty);
                Add("123", typeof(int), empty, empty);
                Add("new List<string>()", typeof(List<string>), new[] { "System.Collections.Generic" }, empty);
                Add("new JsonArrayAttribute()", typeof(JsonArrayAttribute), new[] { "Newtonsoft.Json" }, new[] { "Newtonsoft.Json" });
            }
        }
        public class VisualBasicInferTypeData : TheoryData<string, Type, string[], string[]>
        {
            public VisualBasicInferTypeData()
            {
                var empty = Array.Empty<string>();
                Add("\"abc\"", typeof(string), empty, empty);
                Add("123", typeof(int), empty, empty);
                Add("New List(Of String)()", typeof(List<string>), new[] { "System.Collections.Generic" }, empty);
                Add("New JsonArrayAttribute()", typeof(JsonArrayAttribute), new[] { "Newtonsoft.Json" }, new[] { "Newtonsoft.Json" });
            }
        }

        [Fact]
        public void Should_compile_CSharp()
        {
            var compiler = new CSharpJitCompiler(new[] { typeof(Expression).Assembly, typeof(Enumerable).Assembly }.ToHashSet());
            var result = compiler.CompileExpression(new ExpressionToCompile("source.Select(s=>s).Sum()", new[] { "System", "System.Linq", "System.Linq.Expressions", "System.Collections.Generic" },
                name => name == "source" ? typeof(List<int>) : null, typeof(int)));
            ((Func<List<int>, int>)result.Compile())(new List<int> { 1, 2, 3 }).ShouldBe(6);
        }

        [Fact]
        public void Should_Fail_VBConversion()
        {
            var compiler = new VbJitCompiler(new[] { typeof(int).Assembly, typeof(Expression).Assembly, typeof(Conversions).Assembly }.ToHashSet());
            new Action(() => compiler.CompileExpression(new ExpressionToCompile("1", new[] { "System", "System.Linq", "System.Linq.Expressions" }, _ => typeof(int), typeof(string))))
                .ShouldThrow<SourceExpressionException>().Message.ShouldContain("BC30512: Option Strict On disallows implicit conversions");
        }

        [Fact]
        public void VBExtraQuote_DoesNotBreakValidation()
        {
            var root = new DynamicActivity();
            var sequence = new Sequence();
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("\"abc\"\"")) });
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("\"valid\"")) });
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("\"valid\"")) });
            ////sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("2")) });
            //sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("\"valid\"")) });
            //sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("\"valid\"")) });
            //sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("\"valid\"")) });
            root.Implementation = () => sequence;

            var validationResult = ActivityValidationServices.Validate(root, new() { ForceExpressionCache = false });
            validationResult.Errors.Count.ShouldBe(1);
        }

        [Fact]
        public void CSExtraQuote_DoesNotBreakValidation()
        {
            var root = new DynamicActivity();
            var sequence = new Sequence();
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("\"valid\"")) });
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("\"valid\"")) });
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("\"valid\"")) });
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("2")) });
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("\"valid\"")) });
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("\"valid\"")) });
            sequence.Activities.Add(new WriteLine { Text = new InArgument<string>(new CSharpValue<string>("\"valid\"")) });
            root.Implementation = () => sequence;

            var validationResult = ActivityValidationServices.Validate(root, new() { ForceExpressionCache = false });
            validationResult.Errors.Count.ShouldBe(1);
        }
    }
    public class AheadOfTimeXamlTests : XamlTestsBase
    {
        protected override bool CompileExpressions => true;

        private const string CSharpExpressions = @"
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
            new Action(() => ActivityXamlServices.Load(new StringReader(CSharpExpressions),
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
                .ShouldAllBe(error => error.Contains("error CS0103: The name 'constant' does not exist in the current context"));
        }
        [Fact]
        public void SetCompiledExpressionRootForImplementation()
        {
            var writeLine = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("[s]")) };
            CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(writeLine, new Expressions());
            WorkflowInvoker.Invoke(writeLine);
        }
        [Fact]
        public void ValidateSkipCompilation()
        {
            var writeLine = new WriteLine { Text = new InArgument<string>(new VisualBasicValue<string>("[s]")) };
            var results = ActivityValidationServices.Validate(writeLine, new() { SkipExpressionCompilation = true });
            results.Errors.ShouldBeEmpty();
        }
        [Fact]
        public void DuplicateVariable()
        {
            var xaml = @"<Activity xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
xmlns:hw='clr-namespace:TestCases.Workflows;assembly=TestCases.Workflows'>
                    <hw:WithMyVar />
                </Activity>";
            var root = Load(xaml);
            var withMyVar = (WithMyVar)WorkflowInspectionServices.Resolve(root, "1.1");
            ((ITextExpression)((Sequence)withMyVar.Body.Handler).Activities[0]).GetExpressionTree();
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

        private class CSharpCompiler : AheadOfTimeCompiler
        {
            public override TextExpressionCompilerResults Compile(ClassToCompile classToCompile) => throw new NotImplementedException();
        }
    }
    internal class Expressions : ICompiledExpressionRoot
    {
        public bool CanExecuteExpression(string expressionText, bool isReference, IList<LocationReference> locations, out int expressionId)
        {
            expressionId = 1;
            return true;
        }
        public Expression GetExpressionTreeForExpression(int expressionId, IList<LocationReference> locationReferences)
        {
            throw new NotImplementedException();
        }
        public string GetLanguage()
        {
            throw new NotImplementedException();
        }
        public IList<string> GetRequiredLocations(int expressionId) => Array.Empty<string>();
        public object InvokeExpression(int expressionId, IList<LocationReference> locations, ActivityContext activityContext) => "";
        public object InvokeExpression(int expressionId, IList<Location> locations)
        {
            throw new NotImplementedException();
        }
    }
    public class WithMyVar : NativeActivity
    {
        public Variable<float> MyVar { get; } = new("MyVar");
        public ActivityAction<DateTime> Body { get; set; } = new()
        {
            Argument = new("MyVar"),
            Handler = new Sequence { Activities = { new VisualBasicValue<DateTime>("MyVar.AddDays(1)") } }
        };
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            AddVariable(MyVar);
            AddDelegate(Body);
            base.CacheMetadata(metadata);
        }
        protected override void Execute(NativeActivityContext context) => context.ScheduleAction(Body, DateTime.Now);
    }

    public class ClassWithDictionaryProperty
    {
        public Dictionary<string, string> TestProperty { get; set; }
    }
}