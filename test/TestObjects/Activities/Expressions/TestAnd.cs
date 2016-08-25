// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities.Expressions
{
    public class TestAnd<TLeft, TRight, TResult> : TestActivity, ITestBinaryExpression<TLeft, TRight, TResult>
    {
        private TestActivity _leftTestActivity;
        private TestActivity _rightTestActivity;

        public TestAnd()
        {
            this.ProductActivity = new And<TLeft, TRight, TResult>();
        }

        public TestAnd(TLeft left, TRight right)
            : this()
        {
            Left = left;
            Right = right;
        }

        public And<TLeft, TRight, TResult> ProductAnd
        {
            get { return this.ProductActivity as And<TLeft, TRight, TResult>; }
        }

        public TLeft Left
        {
            set
            {
                ProductAnd.Left = value;
            }
        }

        public TRight Right
        {
            set
            {
                ProductAnd.Right = value;
            }
        }

        public TestActivity LeftActivity
        {
            get
            {
                return _leftTestActivity;
            }
            set
            {
                if (!(value.ProductActivity is Activity<TLeft>))
                {
                    throw new Exception("LeftActivity.ProductActivity should be Activity<TLeft>");
                }
                ProductAnd.Left = value.ProductActivity as Activity<TLeft>;
                _leftTestActivity = value;
            }
        }

        public TestActivity RightActivity
        {
            get
            {
                return _rightTestActivity;
            }
            set
            {
                if (!(value.ProductActivity is Activity<TRight>))
                {
                    throw new Exception("RightActivity.ProductActivity should be Activity<TRight>");
                }
                ProductAnd.Right = value.ProductActivity as Activity<TRight>;
                _rightTestActivity = value;
            }
        }

        public Variable<TResult> Result
        {
            set
            {
                ProductAnd.Result = value;
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_leftTestActivity != null)
            {
                yield return _leftTestActivity;
            }

            if (_rightTestActivity != null)
            {
                yield return _rightTestActivity;
            }
        }
    }
}
