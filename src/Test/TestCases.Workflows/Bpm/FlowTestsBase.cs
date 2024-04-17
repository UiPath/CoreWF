using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using Activity = System.Activities.Activity;
using WorkflowApplicationTestExtensions;
using JsonFileInstanceStore.Persistence;
using System;

namespace TestCases.Activitiess.Bpm;

public class FlowTestsBase
{
    protected IWorkflowSerializer Serializer { get; set; } = new DataContractWorkflowSerializer();
    protected List<string> TestFlowResults(FlowNode startNode)
    {
        var flowchart = new Flowchart { StartNode = startNode };
        return TestFlowResults(flowchart);
    }

    protected List<string> TestFlowResults(Activity flowchart)
    {
        Variable<List<string>> _stringsVariable = new("strings", c => new());
        var root = new ActivityWithResult<List<string>> { Body = flowchart, In = _stringsVariable };
        var app = new WorkflowApplication(root)
        {
            InstanceStore = new FileStore(Serializer, Environment.CurrentDirectory)
        };
        return (List<string>)app.RunUntilCompletion().Outputs["Result"];
    }

    private class ActivityWithResult<TResult> : NativeActivity<TResult>
    {
        public Variable<TResult> In { get; set; } = new();
        public Activity Body { get; set; }
        protected override void Execute(NativeActivityContext context) => context.ScheduleActivity(Body, (context, _) =>
        {
            TResult value;
            using (context.InheritVariables())
            {
                value = In.Get(context);
            }
            context.SetValue(Result, value);
        });
    }
}
