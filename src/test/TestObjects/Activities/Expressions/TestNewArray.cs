// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities;
using Test.Common.TestObjects.Activities.Collections;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestNewArray<TResult> : TestActivity
    {
        private readonly MemberCollection<Argument> _bounds;
        public TestNewArray()
        {
            _bounds = new MemberCollection<Argument>(AddBound);
            this.ProductActivity = new NewArray<TResult>();
        }

        public Variable<TResult> Result
        {
            set
            {
                ((NewArray<TResult>)this.ProductActivity).Result = value;
            }
        }

        public MemberCollection<Argument> Bounds
        {
            get
            {
                return _bounds;
            }
        }

        private void AddBound(Argument item)
        {
            ((NewArray<TResult>)this.ProductActivity).Bounds.Add(item);
        }
    }
}
