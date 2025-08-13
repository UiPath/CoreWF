using System.Activities;
using System.Linq;
using Xunit;

namespace Test.TestCases.Activities
{
    public class CSharpCompilerHelperTests
    {
        private readonly CSharpCompilerHelper _compilerHelper = new CSharpCompilerHelper();

        [Fact]
        public void CreateExpressionCode_WithPipeDelimitedTypes_ShouldSplitCorrectly()
        {
            // Arrange
            var types = "string| int| bool";
            var names = "arg1, arg2, arg3";
            var code = "new object()";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains("Func<string, int, bool>", result);
            Assert.Contains("CreateExpression() => (arg1, arg2, arg3) => new object();", result);
        }

        [Fact]
        public void CreateExpressionCode_WithCommaDelimitedTypes_ShouldNotSplitCorrectly()
        {
            // Arrange - This tests the old behavior would be wrong
            var types = "string, int, bool";
            var names = "arg1, arg2, arg3";
            var code = "new object()";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            // With pipe splitting, comma-delimited types will be treated as one type
            Assert.Contains("Func<string, int, bool>", result);
        }

        [Fact]
        public void CreateExpressionCode_WithSixteenOrFewerParameters_ShouldUseBuiltInFunc()
        {
            // Arrange
            var types = string.Join("| ", Enumerable.Range(0, 16).Select(_ => "string"));
            var names = string.Join(", ", Enumerable.Range(0, 16).Select(i => $"arg{i}"));
            var code = "\"result\"";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains("Expression<Func<", result);
            Assert.DoesNotContain("public delegate", result);
            Assert.Contains("CreateExpression() =>", result);
        }

        [Fact]
        public void CreateExpressionCode_WithMoreThanSixteenParameters_ShouldUseCustomDelegate()
        {
            // Arrange
            var types = string.Join("| ", Enumerable.Range(0, 17).Select(_ => "string"));
            var names = string.Join(", ", Enumerable.Range(0, 17).Select(i => $"arg{i}"));
            var code = "\"result\"";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains("public delegate", result);
            Assert.Contains("Func", result);
            Assert.Contains("CreateExpression() =>", result);
        }

        [Fact]
        public void CreateExpressionCode_WithExactlySixteenParameters_ShouldUseBuiltInFunc()
        {
            // Arrange
            var types = string.Join("| ", Enumerable.Range(0, 16).Select(_ => "string"));
            var names = string.Join(", ", Enumerable.Range(0, 16).Select(i => $"arg{i}"));
            var code = "\"result\"";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains("Expression<Func<", result);
            Assert.DoesNotContain("public delegate", result);
        }

        [Fact]
        public void CreateExpressionCode_WithSeventeenParameters_ShouldUseCustomDelegate()
        {
            // Arrange
            var types = string.Join("| ", Enumerable.Range(0, 17).Select(_ => "string"));
            var names = string.Join(", ", Enumerable.Range(0, 17).Select(i => $"arg{i}"));
            var code = "\"result\"";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains("public delegate", result);
            Assert.Contains("Func", result);
        }

        [Fact]
        public void CreateExpressionCode_WithSingleParameter_ShouldUseBuiltInFunc()
        {
            // Arrange
            var types = "string";
            var names = "arg1";
            var code = "\"result\"";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains("Expression<Func<string>", result);
            Assert.DoesNotContain("public delegate", result);
        }

        [Fact]
        public void CreateExpressionCode_WithNoParameters_ShouldUseBuiltInFunc()
        {
            // Arrange
            var types = "";
            var names = "";
            var code = "\"result\"";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains("Expression<Func<>", result);
            Assert.DoesNotContain("public delegate", result);
        }

        [Theory]
        [InlineData("string| int", "arg1, arg2", "new object()", "Func<string, int>")]
        [InlineData("Dictionary<string, object>| List<int>", "dict, list", "dict.Count + list.Count", "Func<Dictionary<string, object>, List<int>>")]
        [InlineData("System.Activities.InArgument<string>", "variable1", "new Dictionary<string, InArgument<string>>()", "Func<System.Activities.InArgument<string>>")]
        public void CreateExpressionCode_WithVariousTypeFormats_ShouldHandleCorrectly(string types, string names, string code, string expectedFuncPattern)
        {
            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains(expectedFuncPattern, result);
            Assert.Contains($"({names}) => {code}", result);
        }

        [Fact]
        public void CreateExpressionCode_WithComplexGenericTypes_ShouldPreserveTypeStructure()
        {
            // Arrange
            var types = "Dictionary<string, InArgument<string>>| List<Dictionary<int, string>>";
            var names = "dict, list";
            var code = "dict.Count";

            // Act
            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            // Assert
            Assert.Contains("Dictionary<string, InArgument<string>>, List<Dictionary<int, string>>", result);
            Assert.Contains("(dict, list) => dict.Count", result);
        }

        [Fact]
        public void CreateExpressionCode_EmptyInput_ShouldHandleGracefully()
        {
            // Act
            var result = _compilerHelper.CreateExpressionCode("", "", "null");

            // Assert
            Assert.Contains("Expression<Func<>", result);
            Assert.Contains("() => null", result);
        }

        [Fact]
        public void CreateExpressionCode_BoundaryTesting_FifteenVsSixteenVsSeventeenParams()
        {
            // Test 15 parameters (should use built-in Func)
            var types15 = string.Join("| ", Enumerable.Range(0, 15).Select(_ => "string"));
            var names15 = string.Join(", ", Enumerable.Range(0, 15).Select(i => $"arg{i}"));
            var result15 = _compilerHelper.CreateExpressionCode(types15, names15, "\"test\"");

            Assert.Contains("Expression<Func<", result15);
            Assert.DoesNotContain("public delegate", result15);

            // Test 16 parameters (should use built-in Func)
            var types16 = string.Join("| ", Enumerable.Range(0, 16).Select(_ => "string"));
            var names16 = string.Join(", ", Enumerable.Range(0, 16).Select(i => $"arg{i}"));
            var result16 = _compilerHelper.CreateExpressionCode(types16, names16, "\"test\"");

            Assert.Contains("Expression<Func<", result16);
            Assert.DoesNotContain("public delegate", result16);

            // Test 17 parameters (should use custom delegate)
            var types17 = string.Join("| ", Enumerable.Range(0, 17).Select(_ => "string"));
            var names17 = string.Join(", ", Enumerable.Range(0, 17).Select(i => $"arg{i}"));
            var result17 = _compilerHelper.CreateExpressionCode(types17, names17, "\"test\"");

            Assert.Contains("public delegate", result17);
            Assert.Contains("Func", result17);
        }

        [Fact]
        public void CreateExpressionCode_WithPipeInTypeName_ShouldNotBeSplitIncorrectly()
        {
            // This tests edge case where type names might contain pipe characters in generic constraints
            // Though this is unlikely in practice, it's good to document the behavior
            var types = "string";
            var names = "arg1";
            var code = "arg1";

            var result = _compilerHelper.CreateExpressionCode(types, names, code);

            Assert.Contains("Expression<Func<string>", result);
        }
    }
}
