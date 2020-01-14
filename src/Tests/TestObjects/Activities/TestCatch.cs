// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;

namespace Test.Common.TestObjects.Activities
{
    public abstract class TestCatch : TestActivity
    {
        protected Catch productCatch;
        public bool HintHandleException { get; set; }

        public abstract Type ExceptionType { get; }
        public abstract TestActivity Body { get; set; }

        internal Catch NonGenericProductCatch
        {
            get { return this.productCatch; }
        }

        public override string DisplayName
        {
            get { throw new NotSupportedException("TestCatch is a delegate not an activity, you cant set DisplayName on it."); }
            set { throw new NotSupportedException("TestCatch is a delegate not an activity, you cant set DisplayName on it."); }
        }
    }

    public class TestCatch<TException> : TestCatch where TException : Exception
    {
        private TestActivity _body;

        public TestCatch()
        {
            this.productCatch = new Catch<TException>();
        }

        public DelegateInArgument<TException> ExceptionVariable
        {
            set
            {
                if (this.ProductCatch.Action == null)
                {
                    this.ProductCatch.Action = new ActivityAction<TException>();
                }
                this.ProductCatch.Action.Argument = value;
            }
        }

        public override TestActivity Body
        {
            get { return _body; }
            set
            {
                _body = value;

                if (this.ProductCatch.Action == null)
                {
                    this.ProductCatch.Action = new ActivityAction<TException>();
                }
                this.ProductCatch.Action.Handler = value.ProductActivity;
            }
        }

        public override Type ExceptionType
        {
            get { return typeof(TException); }
        }

        private Catch<TException> ProductCatch
        {
            get { return (Catch<TException>)this.productCatch; }
        }

        internal override System.Collections.Generic.IEnumerable<TestActivity> GetChildren()
        {
            if (_body != null)
            {
                yield return _body;
            }
        }
    }
}
