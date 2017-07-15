// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using CoreWf;
using CoreWf.Statements;
using Test.Common.TestObjects.Activities.Collections;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    // There are two NoPersistScope activities. 
    // The one in System.ServiceModel.Activities is internal and used by messaging activity. Its test object is TestNoPersistScope. 
    // The one in CoreWf.Statements is public. Its test object is TestPublicNoPersistScope. 
    public class TestPublicNoPersistScope : TestActivity
    {
        public TestPublicNoPersistScope()
            : this("NoPersistScope")
        {
        }

        public TestPublicNoPersistScope(string DisplayName)
        {
            this.ProductActivity = new NoPersistScope();
            this.DisplayName = DisplayName;
        }

        private NoPersistScope ProductNoPersistScope
        {
            get
            {
                return (NoPersistScope)this.ProductActivity;
            }
        }

        private TestActivity _body;
        public TestActivity Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;

                if (value == null)
                {
                    this.ProductNoPersistScope.Body = null;
                }
                else
                {
                    this.ProductNoPersistScope.Body = value.ProductActivity;
                }
            }
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (this.Body != null)
            {
                yield return this.Body;
            }
        }
    }
}



