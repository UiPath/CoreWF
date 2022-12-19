// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Activities.Statements;
using System.Activities;
namespace Test.Common.TestObjects.Activities
{
    public abstract class TestBpmElement : TestActivity
    {
        public override Activity ProductActivity
        { 
            get => base.ProductActivity ??= GetProductElement(); 
            protected internal set => base.ProductActivity = value; 
        }
        [DefaultValue(false)]
        public virtual bool IsFaulting
        {
            get;
            set;
        }

        [DefaultValue(false)]
        public virtual bool IsCancelling
        {
            get;
            set;
        }
        public static TestBpmElement FromTestActivity(TestActivity activity) => new TestBpmStep { ActionActivity = activity };

        public abstract BpmNode GetProductElement();

        //This is needed to return the next element based on the hints (for conditional elements)
        public abstract TestBpmElement GetNextElement();
    }
}
