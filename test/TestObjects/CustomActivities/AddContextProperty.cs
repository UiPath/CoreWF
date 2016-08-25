// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Utilities.Validation;
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
