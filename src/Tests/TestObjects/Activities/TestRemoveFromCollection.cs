// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities
{
    // using Microsoft.VisualBasic.Activities;

    public class TestRemoveFromCollection<T> : TestActivity
    {
        public TestRemoveFromCollection()
        {
            this.ProductActivity = new RemoveFromCollection<T>();
        }

        public TestRemoveFromCollection(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public ICollection<T> Collection
        {
            set { this.ProductRemoveFromCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public Expression<Func<ActivityContext, ICollection<T>>> CollectionExpression
        {
            set { this.ProductRemoveFromCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public TestActivity CollectionActivity
        {
            set
            {
                this.ProductRemoveFromCollection.Collection = (Activity<ICollection<T>>)value.ProductActivity;
            }
        }

        public Variable<ICollection<T>> CollectionVariable
        {
            set { this.ProductRemoveFromCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public T Item
        {
            set { this.ProductRemoveFromCollection.Item = new InArgument<T>(value); }
        }

        public Expression<Func<ActivityContext, T>> ItemExpression
        {
            set { this.ProductRemoveFromCollection.Item = new InArgument<T>(value); }
        }

        public Variable<T> ItemVariable
        {
            set { this.ProductRemoveFromCollection.Item = new InArgument<T>(value); }
        }

        public TestActivity ItemActivity
        {
            set { this.ProductRemoveFromCollection.Item = (Activity<T>)value.ProductActivity; }
        }

        public Variable<bool> ResultVariable
        {
            set { this.ProductRemoveFromCollection.Result = new OutArgument<bool>(value); }
        }

        private RemoveFromCollection<T> ProductRemoveFromCollection
        {
            get { return (RemoveFromCollection<T>)this.ProductActivity; }
        }
    }
}
