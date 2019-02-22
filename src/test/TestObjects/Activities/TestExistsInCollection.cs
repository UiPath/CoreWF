// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities
{
    public class TestExistsInCollection<T> : TestActivity
    {
        public TestExistsInCollection()
        {
            this.ProductActivity = new ExistsInCollection<T>();
        }

        public TestExistsInCollection(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public ICollection<T> Collection
        {
            set { this.ProductExistsInCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public Expression<Func<ActivityContext, ICollection<T>>> CollectionExpression
        {
            set { this.ProductExistsInCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public TestActivity CollectionActivity
        {
            set
            {
                this.ProductExistsInCollection.Collection = (Activity<ICollection<T>>)value.ProductActivity;
            }
        }

        public Variable<ICollection<T>> CollectionVariable
        {
            set { this.ProductExistsInCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public T Item
        {
            set { this.ProductExistsInCollection.Item = new InArgument<T>(value); }
        }

        public TestActivity ItemActivity
        {
            set { this.ProductExistsInCollection.Item = (Activity<T>)value.ProductActivity; }
        }

        public Expression<Func<ActivityContext, T>> ItemExpression
        {
            set { this.ProductExistsInCollection.Item = new InArgument<T>(value); }
        }

        public Variable<T> ItemVariable
        {
            set { this.ProductExistsInCollection.Item = new InArgument<T>(value); }
        }

        public Variable<bool> ResultVariable
        {
            set { this.ProductExistsInCollection.Result = new OutArgument<bool>(value); }
        }

        private ExistsInCollection<T> ProductExistsInCollection
        {
            get { return (ExistsInCollection<T>)this.ProductActivity; }
        }
    }
}
