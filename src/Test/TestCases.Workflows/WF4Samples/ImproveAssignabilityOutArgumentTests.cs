using Shouldly;
using System;
using System.Activities;
using System.Activities.XamlIntegration;
using System.Collections.Generic;
using Xunit;

namespace TestCases.Workflows.WF4Samples;

public class ImproveAssignabilityOutArgumentTests : ExpressionsBaseCommon
{
    protected override bool CompileExpressions => true;

    [Fact]
    public void CompileImproveAssignabilityOutArgumentActivity()
    {
        Activity activity = null;
        
        // Assert that ActivityXamlServices.Load does not throw
        Should.NotThrow(() =>
        {
            activity = ActivityXamlServices.Load(TestHelper.GetXamlStream(TestXamls.ImproveAssignabilityOutArgumentActivity),
                new ActivityXamlServicesSettings
                {
                    CompileExpressions = true
                });
        });
        
        var invoker = new WorkflowInvoker(activity);
        var result = invoker.Invoke();
        result["myEnumerable"].ShouldBe(new List<string> { "aaa", "bbb", "ccc" });
    }
}
