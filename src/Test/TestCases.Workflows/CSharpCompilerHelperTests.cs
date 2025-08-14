using System.Activities;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;

namespace TestCases.Workflows
{
    public class CSharpCompilerHelperTests
    {
        private readonly CSharpCompilerHelper _compilerHelper = new CSharpCompilerHelper();

        [Fact]
        public void DefaultCompilationUnit_ShouldNotBeNull()
        {
            // Act & Assert
            _compilerHelper.DefaultCompilationUnit.ShouldNotBeNull();
            _compilerHelper.DefaultCompilationUnit.ShouldBeOfType<CSharpCompilation>();
        }

        [Fact]
        public void IdentifierKind_ShouldReturnCorrectSyntaxKind()
        {
            // Act & Assert
            _compilerHelper.IdentifierKind.ShouldBe((int)SyntaxKind.IdentifierName);
        }

        [Fact]
        public void ScriptParseOptions_ShouldHaveCorrectProperties()
        {
            // Act & Assert
            _compilerHelper.ScriptParseOptions.ShouldNotBeNull();
            _compilerHelper.ScriptParseOptions.Kind.ShouldBe(SourceCodeKind.Script);
            _compilerHelper.ScriptParseOptions.ShouldBeOfType<CSharpParseOptions>();
        }

        [Fact]
        public void IdentifierNameComparer_ShouldBeOrdinal()
        {
            // Act & Assert
            _compilerHelper.IdentifierNameComparer.ShouldBe(StringComparer.Ordinal);
        }

        [Fact]
        public void IdentifierNameComparison_ShouldBeOrdinal()
        {
            // Act & Assert
            _compilerHelper.IdentifierNameComparison.ShouldBe(StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(typeof(string), "string")]
        [InlineData(typeof(int), "int")]
        [InlineData(typeof(bool), "bool")]
        [InlineData(typeof(List<string>), "List<string>")]
        [InlineData(typeof(Dictionary<string, int>), "Dictionary<string, int>")]
        public void GetTypeName_ShouldReturnCorrectTypeNames(Type type, string expectedTypeName)
        {
            // Act
            var result = _compilerHelper.GetTypeName(type);

            // Assert
            result.ShouldBe(expectedTypeName);
        }

        [Fact]
        public void CreateExpressionCode_WithSimpleParameters_ShouldGenerateCorrectCode()
        {
            // Arrange
            var types = new[] { "string", "int" };
            var names = new[] { "name", "age" };
            var code = "name + age.ToString()";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldBe("public static Expression<Func<string, int>> CreateExpression() => (name, age) => name + age.ToString();");
        }

        [Fact]
        public void CreateExpressionCode_WithSingleParameter_ShouldGenerateCorrectCode()
        {
            // Arrange
            var types = new[] { "string" };
            var names = new[] { "input" };
            var code = "input.ToUpper()";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldBe("public static Expression<Func<string>> CreateExpression() => (input) => input.ToUpper();");
        }

        [Fact]
        public void CreateExpressionCode_WithNoParameters_ShouldGenerateCorrectCode()
        {
            // Arrange
            var types = Array.Empty<string>();
            var names = Array.Empty<string>();
            var code = "\"Hello World\"";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldBe("public static Expression<Func<>> CreateExpression() => () => \"Hello World\";");
        }

        [Fact]
        public void CreateExpressionCode_WithMoreThan16Parameters_ShouldGenerateCustomDelegate()
        {
            // Arrange
            var types = Enumerable.Range(0, 17).Select(i => $"T{i}").ToArray();
            var names = Enumerable.Range(0, 17).Select(i => $"arg{i}").ToArray();
            var code = "arg0";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldContain("public delegate TResult Func");
            result.ShouldContain("public static Expression<Func");
            result.ShouldContain(code);
        }

        [Fact]
        public void CreateExpressionCode_WithLessThan16Parameters_ShouldNotGenerateCustomDelegate()
        {
            // Arrange
            var types = Enumerable.Range(0, 5).Select(i => $"T{i}").ToArray();
            var names = Enumerable.Range(0, 5).Select(i => $"arg{i}").ToArray();
            var code = "arg0";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldNotContain("public delegate TResult Func");
            result.ShouldContain("public static Expression<Func");
            result.ShouldContain(code);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(17)]
        [InlineData(22)]
        public void DefineDelegateCommon_ShouldGenerateCorrectDelegate(int argumentsCount)
        {
            // Act
            var (delegateDefinition, delegateName) = _compilerHelper.DefineDelegate(Enumerable.Range(0, argumentsCount + 1).Select(i => $"T{i}"));

            // Assert
            delegateDefinition.ShouldNotBeNullOrEmpty();
            delegateName.ShouldNotBeNullOrEmpty();
            delegateDefinition.ShouldStartWith("public delegate TResult");
            delegateDefinition.ShouldContain(delegateName);
            delegateDefinition.ShouldContain("out TResult");

            // Check parameter count
            var parameterCount = delegateDefinition.Split("in T").Length - 1;
            parameterCount.ShouldBe(argumentsCount);
        }

        [Fact]
        public void DefineDelegate_WithStringArray_ShouldReturnCorrectDelegate()
        {
            // Arrange
            var types = new[] { "string", "int", "bool" };

            // Act
            var (delegateDefinition, delegateName) = _compilerHelper.DefineDelegate(types);

            // Assert
            delegateDefinition.ShouldContain("in T0, in T1,  out TResult");
            delegateName.ShouldStartWith("Func");
        }

        [Fact]
        public void DefineDelegate_WithEnumerableTypes_ShouldReturnCorrectDelegate()
        {
            // Arrange
            var types = new List<string> { "string", "int" };

            // Act
            var (delegateDefinition, delegateName) = _compilerHelper.DefineDelegate(types);

            // Assert
            delegateDefinition.ShouldContain("in T0,  out TResult");
            delegateName.ShouldStartWith("Func");
        }

        [Fact]
        public void DefineDelegate_MultipleCalls_ShouldGenerateUniqueDelegateNames()
        {
            // Arrange
            var types = new[] { "string", "int" };

            // Act
            var (_, delegateName1) = _compilerHelper.DefineDelegate(types);
            var (_, delegateName2) = _compilerHelper.DefineDelegate(types);

            // Assert
            delegateName1.ShouldNotBe(delegateName2);
        }

        [Fact]
        public void DefineDelegate_ObsoleteMethod_ShouldStillWork()
        {
            // Arrange
            var types = "string, int, bool";

            // Act
            var (delegateDefinition, delegateName) = _compilerHelper.DefineDelegate(types);

            // Assert
            delegateDefinition.ShouldNotBeNullOrEmpty();
            delegateName.ShouldNotBeNullOrEmpty();
            delegateDefinition.ShouldContain("in T0, in T1,  out TResult");
        }

        [Fact]
        public void CSharpCompilerHelper_ShouldImplementAllCompilerHelperAbstractMembers()
        {
            // Assert - Verify all abstract members are implemented
            _compilerHelper.DefaultCompilationUnit.ShouldNotBeNull();
            _compilerHelper.ScriptParseOptions.ShouldNotBeNull();
            _compilerHelper.IdentifierNameComparer.ShouldNotBeNull();
            _compilerHelper.IdentifierNameComparison.ShouldBe(StringComparison.Ordinal);
            _compilerHelper.IdentifierKind.ShouldBeGreaterThan(0);

            // Test that GetTypeName works
            var typeName = _compilerHelper.GetTypeName(typeof(string));
            typeName.ShouldNotBeNullOrEmpty();

            // Test that CreateExpressionCode works
            var expressionCode = _compilerHelper.CreateExpressionCode(new[] { "string" }, new[] { "input" }, "input");
            expressionCode.ShouldNotBeNullOrEmpty();
        }
    }
}