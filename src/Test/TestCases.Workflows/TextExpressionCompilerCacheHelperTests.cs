#nullable enable
using Microsoft.CSharp.Activities;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using TestCases.Workflows.TestUtils;
using TestCases.Workflows.WF4Samples;
using Xunit;

namespace TestCases.Workflows;

public class TextExpressionCompilerCacheHelperTests
{
    private const string CacheHelper = "GetCompiledDataContextCacheHelper";
    private const string ReadOnlySuffix = "_ForReadOnly";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task GeneratedSubclasses_NestedAndSiblingScopes_PreserveCacheOwnership(bool forImplementation, bool rootHasVariable)
    {
        var values = new List<int>();
        var caches = new List<CompiledDataContext[]>();
        GeneratedSource? generated = null;
        ICompiledExpressionRoot? compiled = null;
        var sequence = NestedScopes(rootHasVariable, (value, context) =>
        {
            values.Add(value);
            var root = compiled ?? throw new InvalidOperationException("Compiled root is not attached.");
            var source = generated ?? throw new InvalidOperationException("Generated source is not available.");
            var count = GetCacheCount(source.Compiler);
            var activities = GetField<object>(root, "dataContextActivities");
            var arguments = new object[] { activities, context, source.Activity, forImplementation, count };
            var roots = root.GetType().GetNestedTypes(BindingFlags.NonPublic)
                .Where(type => type.BaseType == typeof(CompiledDataContext)).ToArray();
            roots.Length.ShouldBe(2);
            var cache = Assert.IsType<CompiledDataContext[]>(FindCacheHelper(roots[0]).Invoke(null, arguments));
            FindCacheHelper(roots[1]).Invoke(null, arguments).ShouldBeSameAs(cache);
            cache.Length.ShouldBe(count);
            cache.OfType<CompiledDataContext>().ShouldNotBeEmpty();

            // A generated subclass resolves the same static forwarding operation
            // whether the declaration is on itself or on its generated ancestor.
            foreach (var dataContext in cache.OfType<CompiledDataContext>())
            {
                FindCacheHelper(dataContext.GetType()).Invoke(null, arguments).ShouldBeSameAs(cache);
            }
            caches.Add(cache);
        });
        generated = Generate(sequence, "VB", forImplementation);
        compiled = await Compile(generated);
        Attach(generated, compiled);

        WorkflowInvoker.Invoke(generated.Activity);

        var first = rootHasVariable ? 15 : 5;
        values.ShouldBe(new[] { first, first + 1, 12 });
        caches[1].ShouldBeSameAs(caches[0]);
        caches[2].ShouldNotBeSameAs(caches[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GenerateSource_NoRootLocations_ExecutesConstantExpression(bool forImplementation)
    {
        var values = new List<int>();
        var sequence = new Sequence
        {
            Activities =
            {
                new RecordValue((value, _) => values.Add(value))
                {
                    Value = new InArgument<int>(new VisualBasicValue<int>("40 + 2"))
                }
            }
        };
        var generated = Generate(sequence, "VB", forImplementation);
        var compiled = await Compile(generated);
        Attach(generated, compiled);

        WorkflowInvoker.Invoke(generated.Activity);

        values.ShouldBe(new[] { 42 });
    }

    [Fact]
    public async Task GenerateSource_LocationOverloadAndExpressionTree_PreserveValueAndReferenceBinding()
    {
        var variable = new Variable<int>("value", 2);
        var sequence = new Sequence
        {
            Variables = { variable },
            Activities =
            {
                new Assign<int>
                {
                    To = new OutArgument<int>(new VisualBasicReference<int>("value")),
                    Value = new InArgument<int>(new VisualBasicValue<int>("value + 1"))
                }
            }
        };
        var generated = Generate(sequence, "VB", true);
        var compiled = await Compile(generated);
        var references = new List<LocationReference> { variable };
        var location = new Location<int> { Value = 10 };
        var locations = new List<Location> { location };

        compiled.CanExecuteExpression(typeof(int), "value + 1", false, references, out var valueId).ShouldBeTrue();
        Assert.IsType<int>(compiled.InvokeExpression(valueId, locations)).ShouldBe(11);
        var tree = Assert.IsAssignableFrom<LambdaExpression>(compiled.GetExpressionTreeForExpression(valueId, references));
        tree.ReturnType.ShouldBe(typeof(int));
        Assert.Single(tree.Parameters).Type.ShouldBe(typeof(ActivityContext));

        compiled.CanExecuteExpression(typeof(int), "value", true, references, out var referenceId).ShouldBeTrue();
        var bound = Assert.IsAssignableFrom<Location<int>>(compiled.InvokeExpression(referenceId, locations));
        bound.Value.ShouldBe(10);
        bound.Value = 73;
        location.Value.ShouldBe(73);
        Assert.IsType<int>(compiled.InvokeExpression(valueId, locations)).ShouldBe(74);
    }

    [Theory]
    [InlineData("VB", true)]
    [InlineData("C#", false)]
    [InlineData("C#", true)]
    public void GenerateSource_PartialAndCSharp_KeepPerContextForwarders(string language, bool partial)
    {
        Activity<Location<int>> reference = language == "VB"
            ? new VisualBasicReference<int>("value")
            : new CSharpReference<int>("value");
        Activity<int> value = language == "VB"
            ? new VisualBasicValue<int>("value + 1")
            : new CSharpValue<int>("value + 1");
        var sequence = new Sequence
        {
            Variables = { new Variable<int>("value", 1) },
            Activities =
            {
                new Assign<int>
                {
                    To = new OutArgument<int>(reference),
                    Value = new InArgument<int>(value)
                }
            }
        };
        var generated = Generate(sequence, language, true, partial);
        var declaration = GetField<CodeTypeDeclaration>(generated.Compiler, "_classDeclaration");
        var contexts = declaration.Members.OfType<CodeTypeDeclaration>().ToArray();
        contexts.Length.ShouldBeGreaterThan(2);
        foreach (var context in contexts)
        {
            Assert.Single(context.Members.OfType<CodeMemberMethod>().Where(method => method.Name == CacheHelper));
        }
        var calls = CacheCalls(declaration).ToArray();
        calls.ShouldNotBeEmpty();
        Assert.All(calls, call => call.ReceiverType.ShouldBe(call.ContextType));
    }

    [Fact]
    public void GenerateSource_StandaloneVisualBasic_UsesOnlyRootForwarders()
    {
        var generated = Generate(NestedScopes(false, (_, _) => { }), "VB", true);
        var declaration = GetField<CodeTypeDeclaration>(generated.Compiler, "_classDeclaration");
        var contexts = declaration.Members.OfType<CodeTypeDeclaration>().ToArray();
        var roots = contexts.Where(context => context.BaseTypes[0].BaseType == typeof(CompiledDataContext).FullName).ToArray();
        roots.Length.ShouldBe(2);
        contexts.Length.ShouldBeGreaterThan(roots.Length);
        foreach (var context in contexts)
        {
            var helpers = context.Members.OfType<CodeMemberMethod>().Where(method => method.Name == CacheHelper);
            if (roots.Contains(context))
            {
                Assert.Single(helpers);
            }
            else
            {
                helpers.ShouldBeEmpty();
            }
        }
        var calls = CacheCalls(declaration).ToArray();
        calls.ShouldNotBeEmpty();
        calls.ShouldContain(call => call.ContextType.EndsWith(ReadOnlySuffix, StringComparison.Ordinal));
        calls.ShouldContain(call => !call.ContextType.EndsWith(ReadOnlySuffix, StringComparison.Ordinal));
        foreach (var call in calls)
        {
            var readOnly = call.ContextType.EndsWith(ReadOnlySuffix, StringComparison.Ordinal);
            var root = Assert.Single(roots.Where(context => context.Name.EndsWith(ReadOnlySuffix, StringComparison.Ordinal) == readOnly));
            call.ReceiverType.ShouldBe(root.Name);
            call.Count.ShouldBe(GetCacheCount(generated.Compiler));
            call.Index.ShouldBeInRange(0, call.Count - 1);
        }
        foreach (var group in calls.GroupBy(call => call.ContextType))
        {
            Assert.Single(group.Select(call => call.Index).Distinct());
        }
        calls.Select(call => call.Index).Distinct().Count().ShouldBe(calls.Select(call => call.ContextType).Distinct().Count());
    }

    private static Sequence NestedScopes(bool rootHasVariable, Action<int, CodeActivityContext> record)
    {
        var inner = new Sequence
        {
            Variables = { new Variable<int>("second", 3) },
            Activities =
            {
                Assign("SECOND", rootHasVariable ? "FIRST + second + common" : "FIRST + second"),
                Record("second", record),
                Assign("second", "SECOND + 1"),
                Record("SECOND", record)
            }
        };
        var sequence = new Sequence
        {
            Activities =
            {
                new Sequence { Variables = { new Variable<int>("first", 2) }, Activities = { inner } },
                new Sequence
                {
                    Variables = { new Variable<int>("first", 11) },
                    Activities = { Assign("FIRST", "first + 1"), Record("first", record) }
                }
            }
        };
        if (rootHasVariable)
        {
            sequence.Variables.Add(new Variable<int>("common", 10));
        }
        return sequence;
    }

    private static Assign<int> Assign(string target, string value) => new()
    {
        To = new OutArgument<int>(new VisualBasicReference<int>(target)),
        Value = new InArgument<int>(new VisualBasicValue<int>(value))
    };

    private static RecordValue Record(string expression, Action<int, CodeActivityContext> record) => new(record)
    {
        Value = new InArgument<int>(new VisualBasicValue<int>(expression))
    };

    private static GeneratedSource Generate(Sequence sequence, string language, bool forImplementation, bool partial = false)
    {
        Activity activity = forImplementation ? new DynamicActivity { Implementation = () => sequence } : sequence;
        var className = "CacheHelper_" + Guid.NewGuid().ToString("N");
        var compiler = new TextExpressionCompiler(new TextExpressionCompilerSettings
        {
            Activity = activity,
            ActivityName = className,
            Language = language,
            GenerateAsPartialClass = partial,
            AlwaysGenerateSource = true,
            ForImplementation = forImplementation
        });
        using var writer = new StringWriter();
        compiler.GenerateSource(writer).ShouldBeTrue();
        return new GeneratedSource(activity, compiler, className, writer.ToString(), language, forImplementation);
    }

    private static async Task<ICompiledExpressionRoot> Compile(GeneratedSource generated)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic)
            .Select(References.GetReference).ToArray();
        var type = await new Project(references).Compile(generated.Source, generated.ClassName,
            generated.Language == "VB" ? Language.VisualBasic : Language.CSharp);
        return Assert.IsAssignableFrom<ICompiledExpressionRoot>(Activator.CreateInstance(type, generated.Activity));
    }

    private static void Attach(GeneratedSource generated, ICompiledExpressionRoot compiled)
    {
        if (generated.ForImplementation)
        {
            CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(generated.Activity, compiled);
        }
        else
        {
            CompiledExpressionInvoker.SetCompiledExpressionRoot(generated.Activity, compiled);
        }
    }

    private static int GetCacheCount(TextExpressionCompiler compiler)
    {
        var method = typeof(TextExpressionCompiler).GetMethod("GetCacheIndices", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Cache index generator was not found.");
        return Assert.IsType<Dictionary<string, int>>(method.Invoke(compiler, null)).Count;
    }

    private static MethodInfo FindCacheHelper(Type context)
    {
        for (Type? current = context; current != null; current = current.BaseType)
        {
            var method = current.GetMethod(CacheHelper, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (method != null)
            {
                return method;
            }
        }
        throw new InvalidOperationException("Generated cache helper was not found.");
    }

    private static T GetField<T>(object owner, string name)
    {
        var field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Field was not found: " + name);
        return Assert.IsAssignableFrom<T>(field.GetValue(owner));
    }

    private static IEnumerable<CacheCall> CacheCalls(CodeTypeDeclaration declaration) =>
        declaration.Members.OfType<CodeMemberMethod>().Where(method => method.Name == "InvokeExpression")
            .SelectMany(method => CacheCalls(method.Statements));

    private static IEnumerable<CacheCall> CacheCalls(CodeStatementCollection statements)
    {
        for (var index = 0; index < statements.Count; index++)
        {
            var statement = statements[index];
            if (statement is CodeVariableDeclarationStatement variable &&
                variable.InitExpression is CodeMethodInvokeExpression invocation && invocation.Method.MethodName == CacheHelper)
            {
                var condition = Assert.IsType<CodeConditionStatement>(statements[index + 1]);
                var assignment = Assert.IsType<CodeAssignStatement>(Assert.Single(condition.TrueStatements.Cast<CodeStatement>()));
                var create = Assert.IsType<CodeObjectCreateExpression>(assignment.Right);
                var slot = Assert.IsType<CodeIndexerExpression>(assignment.Left);
                invocation.Parameters.Count.ShouldBe(5);
                yield return new CacheCall(
                    Assert.IsType<CodeTypeReferenceExpression>(invocation.Method.TargetObject).Type.BaseType,
                    create.CreateType.BaseType,
                    Assert.IsType<int>(Assert.IsType<CodePrimitiveExpression>(slot.Indices[0]).Value),
                    Assert.IsType<int>(Assert.IsType<CodePrimitiveExpression>(invocation.Parameters[4]).Value));
            }
            if (statement is CodeConditionStatement branch)
            {
                foreach (var call in CacheCalls(branch.TrueStatements).Concat(CacheCalls(branch.FalseStatements)))
                {
                    yield return call;
                }
            }
        }
    }

    private sealed class RecordValue : CodeActivity
    {
        private readonly Action<int, CodeActivityContext> _record;
        public RecordValue(Action<int, CodeActivityContext> record) => _record = record;
        public InArgument<int> Value { get; set; } = new();
        protected override void Execute(CodeActivityContext context) => _record(Value.Get(context), context);
    }

    private sealed record GeneratedSource(Activity Activity, TextExpressionCompiler Compiler, string ClassName,
        string Source, string Language, bool ForImplementation);

    private sealed record CacheCall(string ReceiverType, string ContextType, int Index, int Count);
}
