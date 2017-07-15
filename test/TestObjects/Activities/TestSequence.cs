// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CoreWf;
using CoreWf.Statements;
using Test.Common.TestObjects.Activities.Collections;

namespace Test.Common.TestObjects.Activities
{
    public class TestSequence : TestActivity
    {
        protected MemberCollection<TestActivity> activities;

        public TestSequence()
        {
            this.ProductActivity = new Sequence();
            this.activities = new MemberCollection<TestActivity>(AddActivity);
            this.activities.RemoveItem = RemoveActivity;
            this.activities.RemoveAtItem = RemoveAtActivity;
            this.activities.InsertItem = InsertActivity;
        }

        public TestSequence(string displayName)
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

        private Sequence ProductSequence
        {
            get
            {
                return (Sequence)this.ProductActivity;
            }
        }

        protected void AddActivity(TestActivity item)
        {
            ((Sequence)ProductActivity).Activities.Add(item.ProductActivity);
        }

        protected void InsertActivity(int index, TestActivity item)
        {
            ((Sequence)ProductActivity).Activities.Insert(index, item.ProductActivity);
        }

        protected void RemoveAtActivity(int index)
        {
            ((Sequence)ProductActivity).Activities.RemoveAt(index);
        }

        protected bool RemoveActivity(TestActivity item)
        {
            return ((Sequence)ProductActivity).Activities.Remove(item.ProductActivity);
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            return this.Activities;
        }
    }
}
