using Shouldly;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace TestCases.Workflows
{
    public class JitCompilerTests
    {
        private readonly CSharpJitCompiler _csJitCompiler;
        private readonly VbJitCompiler _vbJitCompiler;
        private readonly string[] _namespaces;

        public JitCompilerTests()
        {
            _namespaces = new[] { "TestCases.Workflows", "System", "System.Linq", "System.Linq.Expressions", "System.Collections.Generic" };
            _vbJitCompiler = new(new HashSet<Assembly> { typeof(string).Assembly, typeof(ClassWithIndexer).Assembly, typeof(Expression).Assembly, typeof(Enumerable).Assembly });
            _csJitCompiler = new(new HashSet<Assembly> { typeof(string).Assembly, typeof(ClassWithIndexer).Assembly, typeof(Expression).Assembly, typeof(Enumerable).Assembly });
        }

        [Fact]
        public void VisualBasicJitCompiler_PropertyAccess()
        {
            var expressionToCompile = "testIndexerClass.Indexer(indexer)";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));
            ((Func<TestIndexerClass, string, string>)result.Compile())(new TestIndexerClass(), "index").ShouldBe("index");
        }

        [Fact]
        public void VisualBasicJitCompiler_MethodCall()
        {
            var expressionToCompile = "testIndexerClass.Method(method)";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));
            ((Func<TestIndexerClass, string, string>)result.Compile())(new TestIndexerClass(), "method").ShouldBe("method");
        }

        [Fact]
        public void VisualBasicJitCompiler_FieldAccess()
        {
            var expressionToCompile = "testIndexerClass.Field + field";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));
            ((Func<TestIndexerClass, string, string>)result.Compile())(new TestIndexerClass(), "field").ShouldBe("field");
        }

        [Fact]
        public void VisualBasicJitCompiler_PropertyAccess_SameNameAsVariable()
        {
            static Type VariableTypeGetter(string name)
            {
                return name switch
                {
                    "Indexer" => typeof(TestIndexerClass),
                    _ => typeof(string),
                };
            }

            var expressionToCompile = "Indexer.Indexer(\"indexer\")";
            var result = _vbJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));
            ((Func<TestIndexerClass, string>)result.Compile())(new TestIndexerClass()).ShouldBe("indexer");
        }

        [Fact]
        public void CSharpJitCompiler_PropertyAccess()
        {
            static Type VariableTypeGetter(string name)
            {
                return name switch
                {
                    "testIndexerClass" => typeof(TestIndexerClass),
                    "Indexer" => typeof(int), // consider we have "Indexer" variable declared in the current context.
                    "indexer" => typeof(string),
                    _ => null
                };
            }

            var expressionToCompile = "testIndexerClass.Indexer[indexer]";
            var result = _csJitCompiler.CompileExpression(new ExpressionToCompile(expressionToCompile, _namespaces, VariableTypeGetter, typeof(string)));

            // "Indexer" variable is added as a parameter, but it is fine, that does not trigger a validation error.
            ((Func<TestIndexerClass, int, string, string>)result.Compile())(new TestIndexerClass(), 0, "index").ShouldBe("index");
        }

        private static Type VariableTypeGetter(string name)
            => name switch
            {
                "testIndexerClass" => typeof(TestIndexerClass),
                _ => typeof(string),
            };
    }

    public class ClassWithIndexer
    {
        public ClassWithIndexer() {}
        public string this[string indexer] => indexer;
    }
    
    public class TestIndexerClass
    {
        public string Field = string.Empty;
        public TestIndexerClass() {}
#pragma warning disable CA1822 // Mark members as static
        public ClassWithIndexer Indexer { get => new(); }
        public string Method(string method) => method;
#pragma warning restore CA1822 // Mark members as static
    }
}
