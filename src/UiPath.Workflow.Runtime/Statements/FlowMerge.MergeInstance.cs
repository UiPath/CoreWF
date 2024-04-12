using System.Diagnostics;
using static System.Activities.Statements.Flowchart;
namespace System.Activities.Statements;

public partial class FlowMerge
{
    public class MergeInstance : NodeInstance<FlowMerge>
    {
        public MergeInstance()
        {
            DoNotComplete = true;
        }
        public bool CancelExecuted { get; set; }
        protected override void Execute()
        {
            if (!DoNotComplete)
                return;
            var runningNodes = Flowchart.GetOtherNodes();
            if (Node.Behavior is MergeFirstBehavior && !CancelExecuted)
            {
                Flowchart.CancelNodes(runningNodes);
                CancelExecuted = true;
            }

            if (runningNodes.Count > 0)
            {
                Debug.WriteLine($"{Node}: DoNotComplete");
                return;
            }

            DoNotComplete = false;
            Debug.WriteLine($"{Node}: Next queued");
            Flowchart.EnqueueNodeExecution(Node.Next, EnqueueType.Pop);
        }
    }
}