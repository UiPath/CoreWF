using Xunit;
using System;
using System.IO;
using System.Activities;
using Microsoft.CSharp.Activities;
using System.Activities.Statements;
using Microsoft.VisualBasic.Activities;
using System.Activities.XamlIntegration;

namespace TestCases.Workflows
{
    public class TextExpressionCompilerTests
    {
        [Theory]
        [InlineData("VB")]
        [InlineData("C#")]
        public void EnsureCompilationParity(string expressionLanguage)
        {
            var actualSourceWriter = new StringWriter();
            var currentAsm = typeof(TextExpressionCompilerTests).Assembly;

            TextExpressionCompilerSettings settings = new()
            {
                RootNamespace = null,
                ForImplementation = true,
                ActivityName = "TestClass",
                AlwaysGenerateSource = true,
                Language = expressionLanguage,
                GenerateAsPartialClass = false,
                ActivityNamespace = "TestNamespace",
                Activity = new DynamicActivity { Implementation = () => GetActivity(expressionLanguage) },
            };

            new TextExpressionCompiler(settings).GenerateSource(actualSourceWriter);
            var expectedStream = currentAsm.GetManifestResourceStream($"{currentAsm.GetName().Name}.ExpressionCompilerResults.{GetExpectedResourceName(expressionLanguage)}");

            var actualSource = actualSourceWriter.ToString();
            var expectedSource = new StreamReader(expectedStream).ReadToEnd();
            Assert.Equal(expectedSource, actualSource);
        }

        private static string GetExpectedResourceName(string expressionLanguage)
            => expressionLanguage switch
            {
                "C#" => "CS_ExpressionCompilerResult",
                "VB" => "VB_ExpressionCompilerResult",
                _ => throw new NotImplementedException()
            };

        private static Activity GetActivity(string expressionLanguage)
            => expressionLanguage switch
            {
                "C#" => GetActivity(v1 => new CSharpValue<bool>(v1), v2 => new CSharpValue<string>(v2), v3 => new CSharpReference<string>(v3)),
                "VB" => GetActivity(v1 => new VisualBasicValue<bool>(v1), v2 => new VisualBasicValue<string>(v2), v3 => new VisualBasicReference<string>(v3)),
                _ => throw new NotImplementedException()
            };

        private static Activity GetActivity<T1, T2, T3>(Func<string, T1> boolValueFactory, Func<string, T2> strValueFactory, Func<string, T3> referenceFactory)
            where T1 : TextExpressionBase<bool>
            where T2 : TextExpressionBase<string>
            where T3 : TextExpressionBase<Location<string>>
            => new Sequence()
            {
                Variables =
                {
                    new Variable<string>("var1")
                },
                Activities =
                {
                    new WriteLine() { Text = new InArgument<string>(strValueFactory("var1")) },
                    new Assign() { To = new OutArgument<string>(referenceFactory("var1")), Value = new InArgument<string>("test1") },
                    new If() {
                        Condition = new InArgument<bool>(boolValueFactory("var1 != \"test\"")),
                        Then = new Sequence()
                        {
                            Variables =
                            {
                                new Variable<string>("var2")
                            },
                            Activities =
                            {
                                new Assign() { To = new OutArgument<string>(referenceFactory("var2")), Value = new InArgument<string>("test2") },
                                new WriteLine() { Text = new InArgument<string>(strValueFactory("var2")) },
                            }
                        },
                        Else = new Sequence()
                        {
                            Activities =
                            {
                                new Sequence()
                                {
                                    Activities =
                                    {
                                        new WriteLine() { Text = new InArgument<string>(strValueFactory("test3")) },
                                    }
                                }
                            }
                        }
                    }
                }
            };
    }
}
