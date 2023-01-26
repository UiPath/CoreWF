using System.Activities.Runtime;
using System.Collections.Generic;
using UiPath.Workflow.Runtime;
namespace UiPath.Bpm.Activities;
public abstract class BpmNode : GoToTargetActivity
{
    private string _parentId;
    private BpmFlowchart _owner;
    internal int Index { get; set; } = -1;
    internal BpmFlowchart Owner => _owner;
    protected override void CacheMetadata(NativeActivityMetadata metadata){}
    // Returns true if this is the first time we've visited this node during this pass
    internal bool Open(BpmFlowchart owner, NativeActivityMetadata metadata)
    {
        if (_parentId != null)
        {
            // We've already visited this node during this pass
            if (!ReferenceEquals(_owner, owner))
            {
                metadata.AddValidationError(SR.FlowNodeCannotBeShared(_owner.DisplayName, owner.DisplayName));
            }
            // Whether we found an issue or not we don't want to change
            // the metadata during this pass.
            return false;
        }
        // if owner.ValidateUnconnectedNodes - BpmFlowchart will be responsible for calling OnOpen for all the Nodes (connected and unconnected)
        _owner = owner;
        _parentId = owner.Id;
        Index = -1;
        return true;
    }
    internal abstract void GetConnectedNodes(IList<BpmNode> connections);
    public void TryExecute(BpmNode node, NativeActivityContext context, ActivityInstance completedInstance)
    {
        if (node == null)
        {
            if (context.IsCancellationRequested)
            {
                System.Diagnostics.Debug.Assert(completedInstance != null, "cannot request cancel if we never scheduled any children");
                // we are done but the last child didn't complete successfully
                if (completedInstance.State != ActivityInstanceState.Closed)
                {
                    context.MarkCanceled();
                }
            }
            return;
        }
        if (context.IsCancellationRequested)
        {
            // we're not done and cancel has been requested
            context.MarkCanceled();
            return;
        }
        System.Diagnostics.Debug.Assert(node != null, "caller should validate");
        context.ScheduleActivity(node);
    }
}