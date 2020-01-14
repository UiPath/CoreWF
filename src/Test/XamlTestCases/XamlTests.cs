using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using Shouldly;
using Xunit;

namespace XamlTestCases
{
    using IStringDictionary = IDictionary<string, object>;
    using StringDictionary = Dictionary<string, object>;

    public class XamlTests
    {
        public static IEnumerable<object[]> XamlNoInputs { get; } = new[]
        {
            new object[] { @"
                <Activity x:Class=""WFTemplate""
                            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                            xmlns:s=""clr-namespace:System;assembly=mscorlib""
                            xmlns:s1=""clr-namespace:System;assembly=System""
                            xmlns:sa=""clr-namespace:System.Activities;assembly=System.Activities""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                </Activity>" },
            new object[] { @"
                <Activity x:Class=""WFTemplate""
                            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <Sequence>
                        <WriteLine Text=""HelloWorld"" />
                    </Sequence>
                </Activity>" },
            new object[] { @"
                <Activity x:Class=""WFTemplate""
                            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <Sequence>
                        <WriteLine Text=""HelloWorld"" />
                    </Sequence>
                </Activity>" },
            // This test is broken
            //yield return new object[] { @"
            //    <Activity x:Class=""WFTemplate""
            //              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
            //              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
            //        <Sequence>
            //            <WriteLine>
            //                <Content>
            //                    <Text>""HelloWorld""</Text>
            //                </Content>
            //            </WriteLine>
            //        </Sequence>
            //    </Activity>" },
            new object[] { @"
                <Activity
                            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <Sequence>
                        <Sequence.Variables>
                            <Variable x:TypeArguments=""x:String"" Default=""My variable text"" Name=""MyVar"" />
                        </Sequence.Variables>
                        <WriteLine Text=""[MyVar]"" />
                    </Sequence>
                </Activity>" },
            new object[] { @"
                <Activity x:Class=""WFTemplate""
                            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <WriteLine Text=""HelloWorld"" />
                </Activity>" },
            new object[] { @"
                <Activity
                            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <Sequence>
                        <Sequence.Variables>
                            <Variable x:TypeArguments=""x:String"" Default=""My variable text"" Name=""text"" />
                        </Sequence.Variables>
                        <WriteLine Text=""[text]"" />
                    </Sequence>
                </Activity>" },
        };

        [Theory]
        [MemberData(nameof(XamlNoInputs))]
        public void XamlWorkflowNoInputs(string xamlString) => InvokeWorkflow(xamlString);

        public static IEnumerable<object[]> XamlWithInputs { get; } = new[]
        {
            new object[] { @"
                <Activity x:Class=""WFTemplate""
                            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                            xmlns:s=""clr-namespace:System;assembly=mscorlib""
                            xmlns:s1=""clr-namespace:System;assembly=System""
                            xmlns:sa=""clr-namespace:System.Activities;assembly=System.Activities""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <x:Members>
                        <x:Property Name=""myInput"" Type=""InArgument(x:String)"" />
                    </x:Members>
                    <WriteLine Text=""[myInput]"" />
                </Activity>" },
            new object[] { @"
                <Activity x:Class=""WFTemplate""
                            xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                            xmlns:s=""clr-namespace:System;assembly=mscorlib""
                            xmlns:s1=""clr-namespace:System;assembly=System""
                            xmlns:sa=""clr-namespace:System.Activities;assembly=System.Activities""
                            xmlns:hw=""clr-namespace:XamlTestCases;assembly=XamlTestCases""
                            xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <x:Members>
                        <x:Property Name=""myInput"" Type=""InArgument(x:String)"" />
                    </x:Members>
                    <hw:HelloWorldConsole Text=""[myInput]"" />
                </Activity>" },
        };

        [Theory]
        [MemberData(nameof(XamlWithInputs))]
        public void XamlWorkflowWithInputs(string xamlString) => InvokeWorkflow(xamlString, new StringDictionary { ["myInput"] = "HelloWorld" });

        [Fact]
        public void XamlWorkflowWithInputsOutputs()
        {
            var xamlString = @"
            <Activity x:Class=""WFTemplate""
                      xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                      xmlns:s=""clr-namespace:System;assembly=mscorlib""
                      xmlns:s1=""clr-namespace:System;assembly=System""
                      xmlns:sa=""clr-namespace:System.Activities;assembly=System.Activities""
                      xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                <x:Members>
                    <x:Property Name=""myOutput"" Type=""OutArgument(x:Int32)"" />
                    <x:Property Name=""myInput"" Type=""InArgument(x:Int32)"" />
                </x:Members>
                <Assign>
                    <Assign.To>
                        <OutArgument x:TypeArguments=""x:Int32"">[myOutput]</OutArgument>
                    </Assign.To>
                    <Assign.Value>
                        <InArgument x:TypeArguments=""x:Int32"">[myInput]</InArgument>
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
                    <Activity x:Class=""WFTemplate""
                              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                              xmlns:s=""clr-namespace:System;assembly=mscorlib""
                              xmlns:s1=""clr-namespace:System;assembly=System""
                              xmlns:sa=""clr-namespace:System.Activities;assembly=System.Activities""
                              xmlns:hw=""clr-namespace:XamlTestCases;assembly=XamlTestCases""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <x:Members>
                            <x:Property Name=""myInput"" Type=""InArgument(hw:PersonToGreet)"" />
                        </x:Members>
                        <hw:ActivityWithObjectArgument Input=""[myInput]"" />
                    </Activity>";

            var inputs = new StringDictionary { ["myInput"] = new PersonToGreet { FirstName = "Jane", LastName = "Doe" } };
            var result = InvokeWorkflow(xamlString, inputs);
        }

        private static IStringDictionary InvokeWorkflow(string xamlString, IStringDictionary inputs = null)
        {
            var activity = ActivityXamlServices.Load(new StringReader(xamlString), new ActivityXamlServicesSettings { CompileExpressions = true });
            return WorkflowInvoker.Invoke(activity, inputs ?? new StringDictionary());
        }
    }
}