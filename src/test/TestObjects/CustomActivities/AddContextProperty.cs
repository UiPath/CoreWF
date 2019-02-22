// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using Test.Common.TestObjects.Activities;
using System.Collections.Generic;

namespace Test.Common.TestObjects.CustomActivities
{
    public class AddContextProperty : NativeActivity
    {
        public Activity Body
        {
            get;
            set;
        }

        public object Property
        {
            get;
            set;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            // None
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.Properties.Add(Guid.NewGuid().ToString(), Property);
            context.ScheduleActivity(Body);
        }
    }

    public class TestAddContextProperty : TestActivity
    {
        public TestAddContextProperty()
        {
            this.ProductActivity = new AddContextProperty();
        }

        private TestActivity _body;
        public TestActivity Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                ((AddContextProperty)this.ProductActivity).Body = value.ProductActivity;
            }
        }

        public object Property
        {
            get
            {
                return ((AddContextProperty)this.ProductActivity).Property;
            }
            set
            {
                ((AddContextProperty)this.ProductActivity).Property = value;
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            yield return _body;
        }
    }
}
