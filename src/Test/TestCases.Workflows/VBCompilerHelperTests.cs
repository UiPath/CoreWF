using Microsoft.CodeAnalysis;
using Shouldly;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TestCases.Workflows
{
    public class VBCompilerHelperTests
    {
        private readonly VBCompilerHelper _compilerHelper = new VBCompilerHelper();

        [Fact]
        public void DefaultCompilationUnit_ShouldNotBeNull()
        {
            // Act & Assert
            _compilerHelper.DefaultCompilationUnit.ShouldNotBeNull();
            _compilerHelper.DefaultCompilationUnit.ShouldBeOfType<Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation>();
        }

        [Fact]
        public void IdentifierKind_ShouldReturnCorrectSyntaxKind()
        {
            // Act & Assert
            _compilerHelper.IdentifierKind.ShouldBe((int)Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IdentifierName);
        }

        [Fact]
        public void ScriptParseOptions_ShouldHaveCorrectProperties()
        {
            // Act & Assert
            _compilerHelper.ScriptParseOptions.ShouldNotBeNull();
            _compilerHelper.ScriptParseOptions.Kind.ShouldBe(SourceCodeKind.Script);
            _compilerHelper.ScriptParseOptions.ShouldBeOfType<Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions>();
        }

        [Fact]
        public void IdentifierNameComparer_ShouldBeOrdinalIgnoreCase()
        {
            // Act & Assert
            _compilerHelper.IdentifierNameComparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void IdentifierNameComparison_ShouldBeOrdinalIgnoreCase()
        {
            // Act & Assert
            _compilerHelper.IdentifierNameComparison.ShouldBe(StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(typeof(string), "String")]
        [InlineData(typeof(int), "Integer")]
        [InlineData(typeof(bool), "Boolean")]
        [InlineData(typeof(List<string>), "List(Of String)")]
        [InlineData(typeof(Dictionary<string, int>), "Dictionary(Of String, Integer)")]
        public void GetTypeName_ShouldReturnCorrectVBTypeNames(Type type, string expectedTypeName)
        {
            // Act
            var result = _compilerHelper.GetTypeName(type);

            // Assert
            result.ShouldBe(expectedTypeName);
        }

        [Fact]
        public void CreateExpressionCode_WithSimpleParameters_ShouldGenerateCorrectVBCode()
        {
            // Arrange
            var types = new[] { "String", "Integer" };
            var names = new[] { "name", "age" };
            var code = "name + age.ToString()";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldBe("Public Shared Function CreateExpression() As Expression(Of Func(Of String, Integer))\nReturn Function(name, age) (name + age.ToString())\nEnd Function");
        }

        [Fact]
        public void CreateExpressionCode_WithSingleParameter_ShouldGenerateCorrectVBCode()
        {
            // Arrange
            var types = new[] { "String" };
            var names = new[] { "input" };
            var code = "input.ToUpper()";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldBe("Public Shared Function CreateExpression() As Expression(Of Func(Of String))\nReturn Function(input) (input.ToUpper())\nEnd Function");
        }

        [Fact]
        public void CreateExpressionCode_WithNoParameters_ShouldGenerateCorrectVBCode()
        {
            // Arrange
            var types = Array.Empty<string>();
            var names = Array.Empty<string>();
            var code = "\"Hello World\"";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldBe("Public Shared Function CreateExpression() As Expression(Of Func(Of ))\nReturn Function() (\"Hello World\")\nEnd Function");
        }

        [Fact]
        public void CreateExpressionCode_WithMoreThan16Parameters_ShouldGenerateCustomVBDelegate()
        {
            // Arrange
            var types = Enumerable.Range(0, 17).Select(i => $"T{i}").ToArray();
            var names = Enumerable.Range(0, 17).Select(i => $"arg{i}").ToArray();
            var code = "arg0";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldContain("Public Delegate Function Func");
            result.ShouldContain("Public Shared Function CreateExpression() As Expression(Of Func");
            result.ShouldContain(code);
        }

        [Fact]
        public void CreateExpressionCode_WithLessThan16Parameters_ShouldNotGenerateCustomVBDelegate()
        {
            // Arrange
            var types = Enumerable.Range(0, 5).Select(i => $"T{i}").ToArray();
            var names = Enumerable.Range(0, 5).Select(i => $"arg{i}").ToArray();
            var code = "arg0";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            result.ShouldNotContain("Public Delegate Function Func");
            result.ShouldContain("Public Shared Function CreateExpression() As Expression(Of Func");
            result.ShouldContain(code);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(17)]
        [InlineData(22)]
        public void DefineDelegateCommon_ShouldGenerateCorrectVBDelegate(int argumentsCount)
        {
            // Act
            var (delegateDefinition, delegateName) = _compilerHelper.DefineDelegate(Enumerable.Range(0, argumentsCount + 1).Select(i => $"T{i}"));

            // Assert
            delegateDefinition.ShouldNotBeNullOrEmpty();
            delegateName.ShouldNotBeNullOrEmpty();
            delegateDefinition.ShouldStartWith("Public Delegate Function");
            delegateDefinition.ShouldContain(delegateName);
            delegateDefinition.ShouldContain("Out TResult");

            // Check parameter count
            var parameterCount = delegateDefinition.Split(" In T").Length - 1;
            parameterCount.ShouldBe(argumentsCount);
        }

        [Fact]
        public void DefineDelegate_WithStringArray_ShouldReturnCorrectVBDelegate()
        {
            // Arrange
            var types = new[] { "String", "Integer", "Boolean" };

            // Act
            var (delegateDefinition, delegateName) = _compilerHelper.DefineDelegate(types);

            // Assert
            delegateDefinition.ShouldContain(" In T0, In T1, Out TResult");
            delegateDefinition.ShouldContain("ByVal arg as T0, ByVal arg as T1");
            delegateName.ShouldStartWith("Func");
        }

        [Fact]
        public void DefineDelegate_WithEnumerableTypes_ShouldReturnCorrectVBDelegate()
        {
            // Arrange
            var types = new List<string> { "String", "Integer" };

            // Act
            var (delegateDefinition, delegateName) = _compilerHelper.DefineDelegate(types);

            // Assert
            delegateDefinition.ShouldContain(" In T0, Out TResult");
            delegateDefinition.ShouldContain("ByVal arg as T0");
            delegateName.ShouldStartWith("Func");
        }

        [Fact]
        public void DefineDelegate_MultipleCalls_ShouldGenerateUniqueDelegateNames()
        {
            // Arrange
            var types = new[] { "String", "Integer" };

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
            var types = "String, Integer, Boolean";

            // Act
            var (delegateDefinition, delegateName) = _compilerHelper.DefineDelegate(types);

            // Assert
            delegateDefinition.ShouldNotBeNullOrEmpty();
            delegateName.ShouldNotBeNullOrEmpty();
            delegateDefinition.ShouldContain(" In T0, In T1, Out TResult");
        }

        [Fact]
        public void VBCompilerHelper_ShouldImplementAllCompilerHelperAbstractMembers()
        {
            // Assert - Verify all abstract members are implemented
            _compilerHelper.DefaultCompilationUnit.ShouldNotBeNull();
            _compilerHelper.ScriptParseOptions.ShouldNotBeNull();
            _compilerHelper.IdentifierNameComparer.ShouldNotBeNull();
            _compilerHelper.IdentifierNameComparison.ShouldBe(StringComparison.OrdinalIgnoreCase);
            _compilerHelper.IdentifierKind.ShouldBeGreaterThan(0);

            // Test that GetTypeName works
            var typeName = _compilerHelper.GetTypeName(typeof(string));
            typeName.ShouldNotBeNullOrEmpty();

            // Test that CreateExpressionCode works
            var expressionCode = _compilerHelper.CreateExpressionCode(new[] { "String" }, new[] { "input" }, "input");
            expressionCode.ShouldNotBeNullOrEmpty();
        }
    }
}
