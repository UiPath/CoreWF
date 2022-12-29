using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
namespace System.Activities.Statements;
public class BpmJoin : BpmNode
{
    ValidatingCollection<BpmNode> _branches;
    [DefaultValue(null)]
    public Collection<BpmNode> Branches => _branches ??= ValidatingCollection<BpmNode>.NullCheck();
    [DefaultValue(null)]
    public BpmNode Next { get; set; }
    protected override bool CanInduceIdle => true;
    protected override void CacheMetadata(NativeActivityMetadata metadata) => StateMachineExtension.Install(metadata);
    record JoinState
    {
        public int Count;
    }
    protected override void Execute(NativeActivityContext context)
    {
        var key = $"{nameof(BpmJoin)}_{Id}";
        var state = context.GetInheritedValue<Dictionary<string, object>>("flowchartState");
        var joinState = (JoinState)state.GetValueOrDefault(key);
        if (joinState == null)
        {
            joinState = new() { Count = 1 };
            state.Add(key, joinState);
            context.CreateBookmark(key);
        }
        else
        {
            joinState.Count++;
        }
        if (joinState.Count < Branches.Count)
        {
            return;
        }
        state.Remove(key);
        context.GetExtension<StateMachineExtension>().ResumeBookmark(new(key));
        TryExecute(Next, context, context.CurrentInstance);
    }
    internal override void GetConnectedNodes(IList<BpmNode> connections)
    {
        if (Next != null)
        {
            connections.Add(Next);
        }
    }
}