// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using Test.Common.TestObjects.Activities.Collections;


namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestNew<T> : TestActivity
    {
        private MemberCollection<TestArgument> _arguments;

        public TestNew()
        {
            this.ProductActivity = new New<T>();
            _arguments = new MemberCollection<TestArgument>(AddArgument);
        }

        public MemberCollection<TestArgument> Arguments
        {
            get { return _arguments; }
        }

        protected void AddArgument(TestArgument item)
        {
            ProductNew.Arguments.Add(item.ProductArgument);
        }

        private New<T> ProductNew
        {
            get { return this.ProductActivity as New<T>; }
        }

        public Variable<T> Result
        {
            set
            {
                ProductNew.Result = new OutArgument<T>(value);
            }
        }
    }
}
