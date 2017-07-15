// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Test.Common.TestObjects.Activities.Collections;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class TestTryCatch : TestActivity
    {
        private MemberCollection<TestCatch> _catches;
        private TestActivity _finallyActivity;
        private TestActivity _tryActivity;

        public TestTryCatch()
        {
            this.ProductActivity = new TryCatch();
            _catches = new MemberCollection<TestCatch>(AddCatch);
            _catches.RemoveAtItem = RemoveAtCatch;
            _catches.InsertItem = InsertCatch;
        }

        public TestTryCatch(string displayName)
            : this()
        {
            this.DisplayName = displayName;
        }

        public MemberCollection<TestCatch> Catches
        {
            get { return _catches; }
        }

        public TestActivity Finally
        {
            get { return _finallyActivity; }
            set
            {
                _finallyActivity = value;
                this.ProductTryCatchFinally.Finally = value == null ? null : value.ProductActivity;
            }
        }

        public TestActivity Try
        {
            get { return _tryActivity; }
            set
            {
                _tryActivity = value;
                this.ProductTryCatchFinally.Try = _tryActivity.ProductActivity;
            }
        }

        public Collection<Variable> Variables
        {
            get
            {
                return this.ProductTryCatchFinally.Variables;
            }
        }

        private TryCatch ProductTryCatchFinally
        {
            get { return (TryCatch)this.ProductActivity; }
        }

        protected void AddCatch(TestCatch item)
        {
            this.ProductTryCatchFinally.Catches.Add(item.NonGenericProductCatch);
        }

        internal override IEnumerable<TestActivity> GetChildren()
        {
            if (this.Try != null)
            {
                yield return this.Try;
            }

            foreach (TestCatch catchActivity in this.Catches)
            {
                if (catchActivity != null && catchActivity.Body != null)
                {
                    yield return catchActivity.Body;
                }
            }

            if (this.Finally != null)
            {
                yield return this.Finally;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            // This lets the old Hint still work
            foreach (TestCatch testCatch in this.Catches)
            {
                if (testCatch.HintHandleException)
                {
                    if (!TracingHelper.CatchAnException(testCatch.ExceptionType, Try))
                    {
                        //Log.TraceInternal("Warning- Catch marked as handle, isnt handling exception.");
                    }
                    break;
                }
            }

            if (this.Try != null)
            {
                Outcome tryOutcome = Try.GetTrace(traceGroup);

                CaughtExceptionOutcome ceo = tryOutcome as CaughtExceptionOutcome;

                // if state is catching exception
                if (ceo != null)
                {
                    // look for a catch to handle this exception
                    TestCatch testcatch = GetCorrectCatchForException(ceo.ExceptionType);

                    if (testcatch == null)
                    {
                        CurrentOutcome = tryOutcome;
                    }
                    else
                    {
                        // wipe out the CaughtExceptionOutcome in case no body is set
                        CurrentOutcome = Outcome.Completed;

                        if (testcatch.Body != null)
                        {
                            // our return state is the catches return state
                            CurrentOutcome = testcatch.Body.GetTrace(traceGroup);
                        }
                    }
                }
                else
                {
                    CurrentOutcome = tryOutcome;
                }
            }

            if (!(CurrentOutcome is UncaughtExceptionOutcome) && Finally != null)
            {
                Outcome finallyOutcome = this.Finally.GetTrace(traceGroup);
                if (finallyOutcome.DefaultPropogationState != OutcomeState.Completed)
                {
                    CurrentOutcome = finallyOutcome;
                }
            }
        }

        protected void RemoveAtCatch(int index)
        {
            this.ProductTryCatchFinally.Catches.RemoveAt(index);
        }

        protected void InsertCatch(int index, TestCatch item)
        {
            this.ProductTryCatchFinally.Catches.Insert(index, item.NonGenericProductCatch);
        }

        private TestCatch GetCorrectCatchForException(Type type)
        {
            TestCatch solution = null;

            // what this does it it loops through the catches until it finds an assignable type. (IE a child or match)
            //  To make sure we get the most specific match, it loops through again using the type of the matching catch
            foreach (TestCatch testcatch in _catches)
            {
                if (testcatch.ExceptionType.IsAssignableFrom(type))
                {
                    if (solution == null)
                    {
                        solution = testcatch;
                        break;
                    }
                    else
                    {
                        if (testcatch.ExceptionType.IsAssignableFrom(solution.ExceptionType))
                        {
                            solution = testcatch;
                            break;
                        }
                    }
                }
            }
            return solution;
        }
    }
}
