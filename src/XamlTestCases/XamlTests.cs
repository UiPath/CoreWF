using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace XamlTestCases
{
    public class XamlTests
    {
        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static IEnumerable<object[]> XamlNoInputs
        {
            get
            {
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                              xmlns:s=""clr-namespace:System;assembly=mscorlib""
                              xmlns:s1=""clr-namespace:System;assembly=System""
                              xmlns:sa=""clr-namespace:System.Activities;assembly=System.Activities""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    </Activity>" };
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <Sequence>
                            <WriteLine Text=""HelloWorld"" />
                        </Sequence>
                    </Activity>" };
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <Sequence>
                            <WriteLine Text=""HelloWorld"" />
                        </Sequence>
                    </Activity>" };
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
                //    </Activity>" };
                yield return new object[] { @"
                    <Activity
                              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <Sequence>
                            <Sequence.Variables>
                                <Variable x:TypeArguments=""x:String"" Default=""My variable text"" Name=""MyVar"" />
                            </Sequence.Variables>
                            <WriteLine Text=""[MyVar]"" />
                        </Sequence>
                    </Activity>" };
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <WriteLine Text=""HelloWorld"" />
                    </Activity>" };
                yield return new object[] { @"
                    <Activity
                              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <Sequence>
                            <Sequence.Variables>
                                <Variable x:TypeArguments=""x:String"" Default=""My variable text"" Name=""text"" />
                            </Sequence.Variables>
                            <WriteLine Text=""[text]"" />
                        </Sequence>
                    </Activity>" };
            }
        }

        [Theory]
        [MemberData(nameof(XamlNoInputs))]
        public void XamlWorkflowNoInputs(string xamlString)
        {
            var settings = new ActivityXamlServicesSettings { CompileExpressions = true };
            var activity = ActivityXamlServices.Load(GenerateStreamFromString(xamlString), settings);
            WorkflowInvoker.Invoke(activity);
        }

        public static IEnumerable<object[]> XamlWithInputs
        {
            get
            {
                yield return new object[] { @"
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
                    </Activity>" };
                yield return new object[] { @"
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
                    </Activity>" };
            }
        }

        [Theory]
        [MemberData(nameof(XamlWithInputs))]
        public void XamlWorkflowWithInputs(string xamlString)
        {
            var settings = new ActivityXamlServicesSettings { CompileExpressions = true };
            var activity = ActivityXamlServices.Load(GenerateStreamFromString(xamlString), settings);
            var inputs = new Dictionary<string, object>();
            inputs.Add("myInput", "HelloWorld");
            WorkflowInvoker.Invoke(activity, inputs);
        }

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
            var settings = new ActivityXamlServicesSettings { CompileExpressions = true };
            var activity = ActivityXamlServices.Load(GenerateStreamFromString(xamlString), settings);
            var inputs = new Dictionary<string, object>();
            inputs.Add("myInput", 1);
            var outputs = WorkflowInvoker.Invoke(activity, inputs);
            Assert.Equal(1, outputs.Count);
            Assert.True(outputs.ContainsKey("myOutput"));
            Assert.Equal(1, (int)outputs["myOutput"]);
        }

        [Fact]
        public void XamlWorkflowWithInputObject()
        {
            var xamlString = @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                              xmlns:s=""clr-namespace:System;assembly=mscorlib""
                              xmlns:s1=""clr-namespace:System;assembly=System""
                              xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:hw=""clr-namespace:XamlTestCases;assembly=XamlTestCases""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <x:Members>
                            <x:Property Name=""myInput"" Type=""InArgument(hw:HelloWorld2Input)"" />
                        </x:Members>
                        <hw:HelloWorldConsole2 Input=""[myInput]"" />
                    </Activity>";

            var settings = new ActivityXamlServicesSettings { CompileExpressions = true };
            var activity = ActivityXamlServices.Load(GenerateStreamFromString(xamlString), settings);
            var inputs = new Dictionary<string, object>();
            inputs.Add("myInput", new HelloWorld2Input { FirstName = "Jane", LastName = "Doe" });
            var result = WorkflowInvoker.Invoke(activity, inputs);
        }
    }
}
