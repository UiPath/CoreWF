using System.Activities.Statements;
using System.ComponentModel;
namespace Test.Common.TestObjects.Activities;
public abstract class TestBpmElement : TestActivity
{
    public new BpmNode ProductActivity { get => (BpmNode)base.ProductActivity; set => base.ProductActivity = value; }
    [DefaultValue(false)]
    public virtual bool IsFaulting { get; set; }
    [DefaultValue(false)]
    public virtual bool IsCancelling { get; set; }
    public static TestBpmElement FromTestActivity(TestActivity activity) => new TestBpmStep { ActionActivity = activity };
    //This is needed to return the next element based on the hints (for conditional elements)
    public abstract TestBpmElement GetNextElement();
}