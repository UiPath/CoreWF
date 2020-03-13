using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.IO;
using Shouldly;
using Xunit;

namespace TestCases.Workflows
{
    using IStringDictionary = IDictionary<string, object>;
    using StringDictionary = Dictionary<string, object>;

    public class XamlCSharpTests
    {
        [Fact]
        public void XamlWorkflowCSharpValue()
        {
            var xamlString = @"
                <Activity x:Class=""WFTemplate""
                          xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                          xmlns:mca=""clr-namespace:Microsoft.CSharp.Activities;assembly=System.Activities"">
                    <x:Members>
                        <x:Property Name=""myOutput"" Type=""OutArgument(x:Int32)"" />
                        <x:Property Name=""myInput"" Type=""InArgument(x:Int32)"" />
                    </x:Members>
                    <Sequence>
                        <Assign>
                          <Assign.To>
                            <OutArgument x:TypeArguments=""x:Int32"">
                              <mca:CSharpReference x:TypeArguments=""x:Int32"">myOutput</mca:CSharpReference>
                            </OutArgument>
                          </Assign.To>
                          <Assign.Value>
                            <InArgument x:TypeArguments=""x:Int32"">
                              <mca:CSharpValue x:TypeArguments=""x:Int32"">myInput</mca:CSharpValue>
                            </InArgument>
                          </Assign.Value>
                        </Assign>
                        <WriteLine>
                          <InArgument x:TypeArguments=""x:String"">
                            <mca:CSharpValue x:TypeArguments=""x:String"">myOutput.ToString()</mca:CSharpValue>
                          </InArgument>
                        </WriteLine>
                    </Sequence>
                </Activity>";

            var inputs = new StringDictionary { ["myInput"] = 5 };
            var outputs = InvokeWorkflow(xamlString, inputs);
            outputs.Count.ShouldBe(1);
            outputs.ContainsKey("myOutput").ShouldBeTrue();
            outputs["myOutput"].ShouldBe(5);
        }

        [Fact]
        public void XamlWorkflowCSharpValueCompilerError()
        {
            var xamlString = @"
                <Activity x:Class=""WFTemplate""
                          xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
                          xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                          xmlns:mca=""clr-namespace:Microsoft.CSharp.Activities;assembly=System.Activities"">
                    <Sequence>
                        <WriteLine>
                          <InArgument x:TypeArguments=""x:String"">
                            <mca:CSharpValue x:TypeArguments=""x:String"">myOutput.ToString()</mca:CSharpValue>
                          </InArgument>
                        </WriteLine>
                    </Sequence>
                </Activity>";

            try
            {
                var outputs = InvokeWorkflow(xamlString);
                outputs.ShouldBeNull("Compilation should throw an exception");
            }
            catch (Exception e)
            {
                e.Message.ShouldStartWith("Compilation failures occurred");
                e.Message.ShouldContain("The name 'myOutput' does not exist in the current context");
            }
        }

        private static IStringDictionary InvokeWorkflow(string xamlString, IStringDictionary inputs = null)
        {
            var activity = ActivityXamlServices.Load(new StringReader(xamlString), new ActivityXamlServicesSettings { CompileExpressions = true });
            return WorkflowInvoker.Invoke(activity, inputs ?? new StringDictionary());
        }
    }
}