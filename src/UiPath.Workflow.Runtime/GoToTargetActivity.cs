using System.Activities;
namespace UiPath.Workflow.Runtime;
public abstract class GoToTargetActivity : NativeActivity
{
    sealed internal override bool CanBeScheduledBy(Activity parent) => true;
}