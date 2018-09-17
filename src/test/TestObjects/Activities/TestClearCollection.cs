// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities
{
    public class TestClearCollection<T> : TestActivity
    {
        public TestClearCollection()
        {
            this.ProductActivity = new ClearCollection<T>();
        }

        public TestClearCollection(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public ICollection<T> Collection
        {
            set { this.ProductClearCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public TestActivity CollectionActivity
        {
            set
            {
                this.ProductClearCollection.Collection = (Activity<ICollection<T>>)value.ProductActivity;
            }
        }

        public Expression<Func<ActivityContext, ICollection<T>>> CollectionExpression
        {
            set { this.ProductClearCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public Variable<ICollection<T>> CollectionVariable
        {
            set { this.ProductClearCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        private ClearCollection<T> ProductClearCollection
        {
            get { return (ClearCollection<T>)this.ProductActivity; }
        }
    }
}
