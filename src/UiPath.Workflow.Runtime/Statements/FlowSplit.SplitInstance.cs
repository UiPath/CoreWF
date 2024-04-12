namespace System.Activities.Statements;

public partial class FlowSplit
{
    public class SplitInstance : Flowchart.NodeInstance<FlowSplit>
    {
        protected override void Execute()
        {
            for (int i = Node.Branches.Count - 1; i >= 0; i--)
            {
                var branch = Node.Branches[i];
                Flowchart.EnqueueNodeExecution(branch, Flowchart.EnqueueType.Push);
            }
        }
    }
}