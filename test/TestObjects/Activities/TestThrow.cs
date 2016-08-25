// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf.Statements;
using Test.Common.TestObjects.Utilities.Validation;
using Test.Common.TestObjects.Activities.Tracing;
using System.Collections.Generic;
using Test.Common.TestObjects.Utilities;

namespace Test.Common.TestObjects.Activities
{
    public class TestThrow<TException> : TestActivity
        where TException : Exception, new()
    {
        public TestThrow()
        {
            this.ProductActivity = new Throw();

            this.ProductThrow.Exception = new InArgument<Exception>(context => new TException());

            //if (TestParameters.IsPartialTrustRun)
            //{
            this.ProductThrow.Exception = new InArgument<Exception>(context => new TException());
            //}
            ////Since the Lambda expression of type Exception is not being XamlRoundTriped , VisualbasicValue is used instead
            //else
            //{
            //    // this.ProductThrow.Exception = new InArgument<Exception>(new VisualBasicValue<Exception>(string.Format("New {0}()", typeof(TException).Name)));
            //    // The following adds the assembly references of TException and all of it's parents to the activity. this is needed for XamlRoundTrip to work. 
            //    List<Type> types = new List<Type>();
            //    Type type = typeof(TException);
            //    types.Add(type);
            //    //while ((type = type.BaseType) != null)
            //    //{
            //    //    types.Add(type);
            //    //}
            //    // VisualBasicUtility.AttachVisualBasicSettingsProperty(this.ProductActivity, types);
            //}

            this.ExpectedOutcome = Outcome.UncaughtException(typeof(TException));
        }

        public TestThrow(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public Expression<Func<ActivityContext, Exception>> ExceptionExpression
        {
            set { this.ProductThrow.Exception = new InArgument<Exception>(value); }
        }

        public Variable<Exception> ExceptionVariable
        {
            set { this.ProductThrow.Exception = new InArgument<Exception>(value); }
        }

        public Activity<Exception> ExceptionActivity
        {
            set { this.ProductThrow.Exception = new InArgument<Exception>(value); }
        }

        public Throw ProductThrow
        {
            get
            {
                return (Throw)this.ProductActivity;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            CaughtExceptionOutcome ceo = ExpectedOutcome as CaughtExceptionOutcome;

            // stuff the exception type in so we dont always have to do that manually
            if (ceo != null)
            {
                if (ceo.ExceptionType == null)
                {
                    ceo.ExceptionType = typeof(TException);
                }
            }
            else
            {
                UncaughtExceptionOutcome ueo = ExpectedOutcome as UncaughtExceptionOutcome;
                if (ueo != null && ueo.ExceptionType == null)
                {
                    ueo.ExceptionType = typeof(TException);
                }
            }

            base.GetActivitySpecificTrace(traceGroup);
        }
    }
}
