// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Statements;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Test.Common.TestObjects.Activities
{
    public class TestAddToCollection<T> : TestActivity
    {
        public TestAddToCollection()
        {
            this.ProductActivity = new AddToCollection<T>();
        }

        public TestAddToCollection(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public ICollection<T> Collection
        {
            set { this.ProductAddToCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public Expression<Func<ActivityContext, ICollection<T>>> CollectionExpression
        {
            set { this.ProductAddToCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public TestActivity CollectionActivity
        {
            set
            {
                this.ProductAddToCollection.Collection = (Activity<ICollection<T>>)value.ProductActivity;
            }
        }

        public Variable<ICollection<T>> CollectionVariable
        {
            set { this.ProductAddToCollection.Collection = new InArgument<ICollection<T>>(value); }
        }

        public T Item
        {
            set { this.ProductAddToCollection.Item = new InArgument<T>(value); }
        }

        public TestActivity ItemActivity
        {
            set { this.ProductAddToCollection.Item = (Activity<T>)value.ProductActivity; }
        }

        public Expression<Func<ActivityContext, T>> ItemExpression
        {
            set { this.ProductAddToCollection.Item = new InArgument<T>(value); }
        }

        public Variable<T> ItemVariable
        {
            set { this.ProductAddToCollection.Item = new InArgument<T>(value); }
        }

        private AddToCollection<T> ProductAddToCollection
        {
            get { return (AddToCollection<T>)this.ProductActivity; }
        }
    }
}
