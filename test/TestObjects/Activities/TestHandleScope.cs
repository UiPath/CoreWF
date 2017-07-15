// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf;
using CoreWf.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Test.Common.TestObjects.Activities.Collections;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestHandleScope<THandle> : TestActivity
        where THandle : Handle
    {
        private TestActivity _body;

        public TestHandleScope()
        {
            this.ProductActivity = new HandleScope<THandle>();
        }

        public TestHandleScope(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public HandleScope<THandle> ProductHandleScope
        {
            get
            {
                return (HandleScope<THandle>)this.ProductActivity;
            }
        }

        public InArgument<THandle> Handle
        {
            get
            {
                return this.ProductHandleScope.Handle;
            }
            set
            {
                this.ProductHandleScope.Handle = value;
            }
        }

        public TestActivity Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
                this.ProductHandleScope.Body = value.ProductActivity;
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (_body != null)
            {
                yield return _body;
            }
        }
    }
}
