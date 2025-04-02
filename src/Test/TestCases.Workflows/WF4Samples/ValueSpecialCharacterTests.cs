using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Activities;
using Shouldly;
using Xunit;

namespace TestCases.Workflows.WF4Samples
{
    //https://uipath.atlassian.net/browse/STUD-74571
    //Tests a workflow that uses "Value" keyword (both C# and vb.net).  Steps:
    //1. Define a variable named "value" (or "Value" for vb.net, since it's not case sensitive). Set it to "KeyName"
    //2. Define a Dictionary as variable - Dict.
    //3. Assign: Dict(Value) = "ValueName"    (Dict[value] = "ValueName")
    //4. Assign: output = Dict.First.Key. => we expect the key is "KeyName" 
    //Results: the output has the unexpected value of "ValueName", instead of "KeyName", but if we enable parameter rename,
    //everything works as expected
    public class ValueSpecialCharacterTests : ExpressionsBase
    {
        protected override bool CompileExpressions => true;

        [Fact]
        public void CompileSpecialCharactersVb()
        {
            var activity = ActivityXamlServices.Load(TestHelper.GetXamlStream(TestXamls.ValueSpecialCharacterVb),
                new ActivityXamlServicesSettings
                {
                    CompileExpressions = true,
                    EnableFunctionParameterRename = true,
                });
            var invoker = new WorkflowInvoker(activity);
            var result = invoker.Invoke();
            result["output"].ShouldBe("KeyName");
        }

        [Fact]
        public void CompileSpecialCharactersCSharp()
        {
            var activity = ActivityXamlServices.Load(TestHelper.GetXamlStream(TestXamls.ValueSpecialCharacterCSharp),
                new ActivityXamlServicesSettings
                {
                    CompileExpressions = true,
                    EnableFunctionParameterRename = true,
                });
            var invoker = new WorkflowInvoker(activity);
            var result = invoker.Invoke();
            result["output"].ShouldBe("KeyName");
        }

        #region Tests old behavior

        /// <summary>
        /// In case we don't enable parameter rename - the output is wrong
        /// </summary>
        [Fact]
        public void CompileSpecialCharactersCSharp_FailsWithRenameDisabled()
        {
            var activity = ActivityXamlServices.Load(TestHelper.GetXamlStream(TestXamls.ValueSpecialCharacterCSharp), 
                new ActivityXamlServicesSettings 
                { 
                    CompileExpressions = true
                });
            var invoker = new WorkflowInvoker(activity);
            var result = invoker.Invoke();
            result["output"].ShouldNotBe("KeyName");
        }

        /// <summary>
        /// In case we don't enable parameter rename - the output is wrong
        /// </summary>
        [Fact]
        public void CompileSpecialCharactersVb_FailsWithRenameDisabled()
        {
            var activity = ActivityXamlServices.Load(TestHelper.GetXamlStream(TestXamls.ValueSpecialCharacterVb),
                new ActivityXamlServicesSettings
                {
                    CompileExpressions = true
                });
            var invoker = new WorkflowInvoker(activity);
            var result = invoker.Invoke();
            result["output"].ShouldNotBe("KeyName");
        }

        #endregion


    }
}
