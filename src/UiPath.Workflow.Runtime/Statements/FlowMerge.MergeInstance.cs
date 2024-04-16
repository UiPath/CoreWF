using System.Diagnostics;
namespace System.Activities.Statements;

public partial class FlowMerge
{
    public class MergeInstance : NodeInstance<FlowMerge>
    {
        public bool MergeCompleted { get; set; }
        public bool CancelExecuted { get; set; }
        protected override void Execute()
        {
            if (MergeCompleted)
                return;
            var runningNodes = Flowchart.GetSameStackNodes();
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

            Debug.WriteLine($"{Node}: Next queued");
            Flowchart.EnqueueNodeExecution(Node.Next, Flowchart.EnqueueType.Pop);
            MergeCompleted = true;
        }
    }
}