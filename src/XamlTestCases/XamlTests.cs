using CoreWf;
using CoreWf.XamlIntegration;
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
                // xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:s=""clr-namespace:System;assembly=mscorlib""
                              xmlns:s1=""clr-namespace:System;assembly=System""
                              xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    </Activity>" };
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:stmt=""clr-namespace:CoreWf.Statements;assembly=CoreWf""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <stmt:Sequence>
                            <stmt:WriteLine Text=""HelloWorld"" />
                        </stmt:Sequence>
                    </Activity>" };
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:stmt=""clr-namespace:CoreWf.Statements;assembly=CoreWf""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <stmt:Sequence>
                            <stmt:WriteLine Text=""HelloWorld"" />
                        </stmt:Sequence>
                    </Activity>" };
                // This test is broken
                //yield return new object[] { @"
                //    <Activity x:Class=""WFTemplate""
                //              xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                //              xmlns:stmt=""clr-namespace:CoreWf.Statements;assembly=CoreWf""
                //              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                //        <stmt:Sequence>
                //            <stmt:WriteLine>
                //                <stmt:Content>
                //                    <stmt:Text>""HelloWorld""</stmt:Text>
                //                </stmt:Content>
                //            </stmt:WriteLine>
                //        </stmt:Sequence>
                //    </Activity>" };
                yield return new object[] { @"
                    <Activity
                              xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:stmt=""clr-namespace:CoreWf.Statements;assembly=CoreWf""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <stmt:Sequence>
                            <stmt:Sequence.Variables>
                                <Variable x:TypeArguments=""x:String"" Default=""My variable text"" Name=""MyVar"" />
                            </stmt:Sequence.Variables>
                            <stmt:WriteLine Text=""[MyVar]"" />
                        </stmt:Sequence>
                    </Activity>" };
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:stmt=""clr-namespace:CoreWf.Statements;assembly=CoreWf""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <stmt:WriteLine Text=""HelloWorld"" />
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
                              xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:stmt=""clr-namespace:CoreWf.Statements;assembly=CoreWf""
                              xmlns:s=""clr-namespace:System;assembly=mscorlib""
                              xmlns:s1=""clr-namespace:System;assembly=System""
                              xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                        <x:Members>
                            <x:Property Name=""myInput"" Type=""InArgument(x:String)"" />
                        </x:Members>
                        <stmt:WriteLine Text=""[myInput]"" />
                    </Activity>" };
                yield return new object[] { @"
                    <Activity x:Class=""WFTemplate""
                              xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                              xmlns:s=""clr-namespace:System;assembly=mscorlib""
                              xmlns:s1=""clr-namespace:System;assembly=System""
                              xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""
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

        [Fact(Skip = "[myOutput] is not recognized as a Location")]
        public void XamlWorkflowWithInputsOutputs()
        {
            var xamlString = @"
            <Activity x:Class=""WFTemplate""
                      xmlns=""clr-namespace:CoreWf;assembly=CoreWf""
                      xmlns:stmt=""clr-namespace:CoreWf.Statements;assembly=CoreWf""
                      xmlns:s=""clr-namespace:System;assembly=mscorlib""
                      xmlns:s1=""clr-namespace:System;assembly=System""
                      xmlns:sa=""clr-namespace:CoreWf;assembly=CoreWf""
                      xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                <x:Members>
                    <x:Property Name=""myOutput"" Type=""OutArgument(x:Int32)"" />
                    <x:Property Name=""myInput"" Type=""InArgument(x:Int32)"" />
                </x:Members>
                <stmt:Assign>
                    <stmt:Assign.To>
                        <OutArgument x:TypeArguments=""x:Int32"">[myOutput]</OutArgument>
                    </stmt:Assign.To>
                    <stmt:Assign.Value>
                        <InArgument x:TypeArguments=""x:Int32"">[myInput]</InArgument>
                    </stmt:Assign.Value>
                </stmt:Assign>
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
    }
}
