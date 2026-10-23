using Microsoft.VisualBasic.Activities;
using Shouldly;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Activities.XamlIntegration;
using System.IO;
using System.Xaml;
using Xunit;

namespace TestCases.Workflows.WF4Samples;

public class LiteralTests
{
    [Fact]
    public void CanConvertToString_NullValue_ReturnsFalse()
    {
        var literal = new Literal<string>(null);
        literal.CanConvertToString(null).ShouldBeFalse();
    }

    [Fact]
    public void CanConvertToString_DefaultLiteral_NullValue_ReturnsFalse()
    {
        var literal = new Literal<string>();
        literal.CanConvertToString(null).ShouldBeFalse();
    }

    [Fact]
    public void CanConvertToString_NonNullString_ReturnsTrue()
    {
        var literal = new Literal<string>("hello");
        literal.CanConvertToString(null).ShouldBeTrue();
    }

    [Fact]
    public void CanConvertToString_EmptyString_ReturnsFalse()
    {
        var literal = new Literal<string>("");
        literal.CanConvertToString(null).ShouldBeFalse();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CanConvertToString_WhitespaceOnlyString_ReturnsFalse(string value)
    {
        var literal = new Literal<string>(value);
        literal.CanConvertToString(null).ShouldBeFalse();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void XamlSerialize_WhitespaceOnlyStringLiteral_WritesLiteralElement(string value)
    {
        var argument = new InArgument<string>(new Literal<string>(value));

        var xaml = XamlServices.Save(argument);

        // Literal declares Value - a string - as its content property, which the XAML parser
        // keeps; InArgument declares Expression, which it does not.
        xaml.ShouldContain("<Literal");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void XamlRoundTrip_WhitespaceOnlyStringLiteral_PreservesTheValue(string value)
    {
        var argument = new InArgument<string>(new Literal<string>(value));

        var xaml = XamlServices.Save(argument);
        var deserialized = (InArgument<string>)XamlServices.Load(new StringReader(xaml));

        deserialized.Expression.ShouldBeOfType<Literal<string>>();
        ((Literal<string>)deserialized.Expression).Value.ShouldBe(value);
    }

    [Fact]
    public void CanConvertToString_StringWithSurroundingWhitespace_ReturnsTrue()
    {
        var literal = new Literal<string>(" x ");
        literal.CanConvertToString(null).ShouldBeTrue();
    }

    [Fact]
    public void CanConvertToString_IntValue_ReturnsTrue()
    {
        var literal = new Literal<int>(42);
        literal.CanConvertToString(null).ShouldBeTrue();
    }

    [Fact]
    public void CanConvertToString_DefaultInt_ReturnsTrue()
    {
        var literal = new Literal<int>(0);
        literal.CanConvertToString(null).ShouldBeTrue();
    }

    [Fact]
    public void CanConvertToString_BoolValue_ReturnsTrue()
    {
        var literal = new Literal<bool>(true);
        literal.CanConvertToString(null).ShouldBeTrue();
    }

    [Fact]
    public void ConvertToString_NonNullString_ReturnsValue()
    {
        var literal = new Literal<string>("hello");
        literal.ConvertToString(null).ShouldBe("hello");
    }

    [Fact]
    public void ConvertToString_IntValue_ReturnsStringRepresentation()
    {
        var literal = new Literal<int>(42);
        literal.ConvertToString(null).ShouldBe("42");
    }

    [Fact]
    public void ConvertToString_BracketedString_EscapesWithPercent()
    {
        var literal = new Literal<string>("[test]");
        literal.ConvertToString(null).ShouldBe("%[test]");
    }

    [Fact]
    public void XamlRoundTrip_NullStringLiteral_DoesNotProduceNothing()
    {
        var literal = new Literal<string>(null);
        string xaml = XamlServices.Save(literal);

        // The serialized XAML should NOT contain [Nothing] (VB-specific null keyword)
        xaml.ShouldNotContain("[Nothing]");

        // Round-trip: deserialize and verify the value is still null
        var deserialized = (Literal<string>)XamlServices.Load(new StringReader(xaml));
        deserialized.Value.ShouldBeNull();
    }

    [Fact]
    public void XamlSerialize_NullArgumentExpression_DoesNotProduceNothing()
    {
        // Reproduces the reported bug: a C# workflow with unused arguments (null)
        // should NOT serialize as [Nothing] (VB syntax)
        var sequence = new Sequence
        {
            Variables = { new Variable<string>("myVar") },
            Activities =
            {
                new Assign<string>
                {
                    To = new OutArgument<string>(new VariableReference<string> { Variable = null }),
                    // This is the key: an InArgument wrapping a null Literal
                    Value = new InArgument<string>(new Literal<string>(null))
                }
            }
        };

        string xaml = XamlServices.Save(sequence);

        // Must not contain VB-specific [Nothing]
        xaml.ShouldNotContain("[Nothing]");
    }

    [Fact]
    public void XamlSerialize_InArgument_NullImplicit_DoesNotProduceNothing()
    {
        // InArgument<string> implicit conversion from null string creates Literal<string>(null)
        InArgument<string> arg = (string)null;

        string xaml = XamlServices.Save(arg);
        xaml.ShouldNotContain("[Nothing]");
    }

    [Fact]
    public void VB_ExistingNothingXaml_StillLoadsCorrectly()
    {
        // Existing VB XAML files containing [Nothing] must still deserialize correctly.
        // [Nothing] is read by ActivityWithResultConverter.ConvertFromString as an expression,
        // creating VisualBasicValue<string>("Nothing") — NOT a Literal<T>.
        // This path is unaffected by the fix.
        var xaml = @"<InArgument x:TypeArguments=""x:String""
            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">[Nothing]</InArgument>";

        var arg = (InArgument<string>)XamlServices.Load(new StringReader(xaml));

        // The expression should be a VisualBasicValue, not a Literal
        arg.Expression.ShouldBeOfType<VisualBasicValue<string>>();
        ((VisualBasicValue<string>)arg.Expression).ExpressionText.ShouldBe("Nothing");
    }

    [Fact]
    public void VB_NullLiteral_RoundTrips_Correctly()
    {
        // A VB workflow with a null Literal<string> should serialize to valid XAML
        // and round-trip back to a Literal<string> with null value.
        var arg = new InArgument<string>(new Literal<string>(null));
        string xaml = XamlServices.Save(arg);

        xaml.ShouldNotContain("[Nothing]");

        var deserialized = (InArgument<string>)XamlServices.Load(new StringReader(xaml));
        // The expression round-trips as a Literal with null value
        deserialized.Expression.ShouldBeOfType<Literal<string>>();
        ((Literal<string>)deserialized.Expression).Value.ShouldBeNull();
    }

    [Fact]
    public void ToString_NullValue_ReturnsNull()
    {
        var literal = new Literal<string>(null);
        literal.ToString().ShouldBe("null");
    }

    [Fact]
    public void ToString_NonNullValue_ReturnsValueString()
    {
        var literal = new Literal<int>(42);
        literal.ToString().ShouldBe("42");
    }

    [Fact]
    public void CSharp_WorkflowWithNullLiteralArg_SerializesAndExecutes()
    {
        // Step 1: Build programmatically and verify serialization doesn't produce [Nothing]
        var temp = new Variable<string>("temp");
        var sequence = new Sequence
        {
            Variables = { temp },
            Activities =
            {
                new Assign<string>
                {
                    To = new OutArgument<string>(temp),
                    Value = new InArgument<string>(new Literal<string>(null))
                }
            }
        };

        string xaml = XamlServices.Save(sequence);
        xaml.ShouldNotContain("[Nothing]");

        // Step 2: Deserialize and verify the null Literal round-tripped correctly
        var deserialized = (Sequence)XamlServices.Load(new StringReader(xaml));
        var assign = (Assign<string>)deserialized.Activities[0];
        assign.Value.Expression.ShouldBeOfType<Literal<string>>();
        ((Literal<string>)assign.Value.Expression).Value.ShouldBeNull();

        // Step 3: Load and execute a C# workflow with a null Literal and verify the If evaluates correctly
        var csXaml = @"
            <Activity x:Class='TestWorkflow'
                      xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                      xmlns:mca='clr-namespace:Microsoft.CSharp.Activities;assembly=System.Activities'>
                <x:Members>
                    <x:Property Name='Result' Type='OutArgument(x:String)' />
                </x:Members>
                <Sequence>
                    <Sequence.Variables>
                        <Variable x:TypeArguments='x:String' Name='temp' />
                    </Sequence.Variables>
                    <Assign x:TypeArguments='x:String'>
                        <Assign.To>
                            <OutArgument x:TypeArguments='x:String'>
                                <mca:CSharpReference x:TypeArguments='x:String'>temp</mca:CSharpReference>
                            </OutArgument>
                        </Assign.To>
                        <Assign.Value>
                            <InArgument x:TypeArguments='x:String'>
                                <Literal x:TypeArguments='x:String' />
                            </InArgument>
                        </Assign.Value>
                    </Assign>
                    <If>
                        <If.Condition>
                            <InArgument x:TypeArguments='x:Boolean'>
                                <mca:CSharpValue x:TypeArguments='x:Boolean'>temp == null</mca:CSharpValue>
                            </InArgument>
                        </If.Condition>
                        <If.Then>
                            <Assign x:TypeArguments='x:String'>
                                <Assign.To>
                                    <OutArgument x:TypeArguments='x:String'>
                                        <mca:CSharpReference x:TypeArguments='x:String'>Result</mca:CSharpReference>
                                    </OutArgument>
                                </Assign.To>
                                <Assign.Value>
                                    <InArgument x:TypeArguments='x:String'>was null</InArgument>
                                </Assign.Value>
                            </Assign>
                        </If.Then>
                        <If.Else>
                            <Assign x:TypeArguments='x:String'>
                                <Assign.To>
                                    <OutArgument x:TypeArguments='x:String'>
                                        <mca:CSharpReference x:TypeArguments='x:String'>Result</mca:CSharpReference>
                                    </OutArgument>
                                </Assign.To>
                                <Assign.Value>
                                    <InArgument x:TypeArguments='x:String'>was not null</InArgument>
                                </Assign.Value>
                            </Assign>
                        </If.Else>
                    </If>
                </Sequence>
            </Activity>";

        var activity = ActivityXamlServices.Load(new StringReader(csXaml),
            new ActivityXamlServicesSettings { CompileExpressions = true });
        var result = WorkflowInvoker.Invoke(activity);
        result["Result"].ShouldBe("was null");
    }

    [Fact]
    public void VB_WorkflowWithNullLiteralArg_SerializesAndExecutes()
    {
        // Step 1: Build programmatically and verify serialization doesn't produce [Nothing]
        var temp = new Variable<string>("temp");
        var sequence = new Sequence
        {
            Variables = { temp },
            Activities =
            {
                new Assign<string>
                {
                    To = new OutArgument<string>(temp),
                    Value = new InArgument<string>(new Literal<string>(null))
                }
            }
        };

        string xaml = XamlServices.Save(sequence);
        xaml.ShouldNotContain("[Nothing]");

        // Step 2: Deserialize and verify the null Literal round-tripped correctly
        var deserialized = (Sequence)XamlServices.Load(new StringReader(xaml));
        var assign = (Assign<string>)deserialized.Activities[0];
        assign.Value.Expression.ShouldBeOfType<Literal<string>>();
        ((Literal<string>)assign.Value.Expression).Value.ShouldBeNull();

        // Step 3: Load and execute a VB workflow with a null Literal and verify the If evaluates correctly
        var vbXaml = @"
            <Activity x:Class='TestWorkflow'
                      xmlns='http://schemas.microsoft.com/netfx/2009/xaml/activities'
                      xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                <x:Members>
                    <x:Property Name='Result' Type='OutArgument(x:String)' />
                </x:Members>
                <Sequence>
                    <Sequence.Variables>
                        <Variable x:TypeArguments='x:String' Name='temp' />
                    </Sequence.Variables>
                    <Assign x:TypeArguments='x:String'>
                        <Assign.To>
                            <OutArgument x:TypeArguments='x:String'>[temp]</OutArgument>
                        </Assign.To>
                        <Assign.Value>
                            <InArgument x:TypeArguments='x:String'>
                                <Literal x:TypeArguments='x:String' />
                            </InArgument>
                        </Assign.Value>
                    </Assign>
                    <If>
                        <If.Condition>
                            <InArgument x:TypeArguments='x:Boolean'>[temp Is Nothing]</InArgument>
                        </If.Condition>
                        <If.Then>
                            <Assign x:TypeArguments='x:String'>
                                <Assign.To>
                                    <OutArgument x:TypeArguments='x:String'>[Result]</OutArgument>
                                </Assign.To>
                                <Assign.Value>
                                    <InArgument x:TypeArguments='x:String'>was Nothing</InArgument>
                                </Assign.Value>
                            </Assign>
                        </If.Then>
                        <If.Else>
                            <Assign x:TypeArguments='x:String'>
                                <Assign.To>
                                    <OutArgument x:TypeArguments='x:String'>[Result]</OutArgument>
                                </Assign.To>
                                <Assign.Value>
                                    <InArgument x:TypeArguments='x:String'>was not Nothing</InArgument>
                                </Assign.Value>
                            </Assign>
                        </If.Else>
                    </If>
                </Sequence>
            </Activity>";

        var activity = ActivityXamlServices.Load(new StringReader(vbXaml));
        var result = WorkflowInvoker.Invoke(activity);
        result["Result"].ShouldBe("was Nothing");
    }
}
