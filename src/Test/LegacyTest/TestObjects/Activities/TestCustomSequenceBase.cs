// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Activities;
using LegacyTest.Test.Common.TestObjects.Activities.Collections;
using LegacyTest.Test.Common.TestObjects.CustomActivities;

namespace LegacyTest.Test.Common.TestObjects.Activities
{
    public class TestCustomSequenceBase : TestActivity
    {
        protected MemberCollection<TestActivity> activities;

        public TestCustomSequenceBase()
        {
            this.ProductActivity = new CustomSequenceBase();
            this.activities = new MemberCollection<TestActivity>(AddActivity);
        }

        public TestCustomSequenceBase(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public IList<Variable> Variables
        {
            get
            {
                return this.ProductSequence.Variables;
            }
        }

        public MemberCollection<TestActivity> Activities
        {
            get
            {
                return this.activities;
            }
        }

        private CustomSequenceBase ProductSequence
        {
            get
            {
                return (CustomSequenceBase)this.ProductActivity;
            }
        }

        protected void AddActivity(TestActivity item)
        {
            ((CustomSequenceBase)ProductActivity).Activities.Add(item.ProductActivity);
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            return this.Activities;
        }
    }
}
