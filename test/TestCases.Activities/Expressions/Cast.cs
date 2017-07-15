// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Collections.Generic;
using Test.Common.TestObjects;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class Cast : IDisposable
    {
        [Fact]
        public void CastBaseTypeToDerivedType()
        {
            Variable<Base> operand = new Variable<Base>() { Name = "Operand" };
            Variable<Derived> result = new Variable<Derived>() { Name = "Derived" };
            Variable<string> output = new Variable<string>() { Name = "Output" };

            TestInvokeMethod invokeMethod = new TestInvokeMethod
            {
                TargetObjectVariable = result,
                MethodName = "MethodInDerivedType"
            };

            invokeMethod.SetResultVariable<string>(output);

            //Base b = new Derived();
            //string result = ((Derived)b).MethodInDerivedType
            TestSequence seq = new TestSequence
            {
                Variables = { operand, result, output },
                Activities =
                {
                    new TestAssign<Base> { ToVariable = operand, ValueExpression = (context => new Derived()) },
                    new TestCast<Base, Derived>
                    {
                        OperandVariable = operand,
                        Result = result
                    },
                    invokeMethod,
                    new TestWriteLine
                    {
                        MessageExpression = (e => output.Get(e)),
                        HintMessage = "Ola"
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestCast<Base, Derived>(),
                                                           new Dictionary<string, object> { { "Operand", new Derived() } },
                                                           new Dictionary<string, object> { { "Result", new Derived() } },
                                                           null);
        }

        [Fact]
        public void DefaultCheckedCast()
        {
            //  
            //  Test case description:
            //  Checked should be defaulted to true.
            Cast<long, short> cast = new Cast<long, short>();

            if (cast.Checked != true)
                throw new Exception("Checked is not defaulted to true");
        }

        [Fact]
        public void CheckedCastOverflow()
        {
            //  
            //  Test case description:
            //  Cast long to short which result in overflow of integer. OverflowException is expected.

            TestCast<long, short> cast = new TestCast<long, short>()
            {
                Checked = true,
                Operand = short.MaxValue + 1L,
                HintExceptionThrown = typeof(OverflowException)
            };

            TestRuntime.RunAndValidateAbortedException(cast, typeof(OverflowException), null);
        }

        //[Fact]
        //public void UnCheckedCastOverflow()
        //{
        //    //  
        //    //  Test case description:
        //    //  unchecked cast long to short two integers which result in overflow.

        //    Variable<short> result = new Variable<short>("result");
        //    TestSequence seq = new TestSequence()
        //    {
        //        Variables =
        //        {
        //            result
        //        },
        //        Activities =
        //        {
        //            new TestCast<long, short>()
        //            {
        //                Checked = false,
        //                Operand = short.MaxValue + 1L,
        //                Result = result
        //            },
        //            new TestWriteLine() { MessageActivity = new TestVisualBasicValue<string>("result.ToString()"), HintMessage = unchecked((short)(short.MaxValue + 1L)).ToString() }
        //        }
        //    };

        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        [Fact]
        public void TryCastingUnrelatedTypes()
        {
            TestCast<string, int> cast = new TestCast<string, int> { Operand = "Hello" };

            string errorMessage = TestExpressionTracer.GetExceptionMessage<string, int>(System.Linq.Expressions.ExpressionType.Convert);

            TestRuntime.ValidateWorkflowErrors(cast, new List<TestConstraintViolation>() { new TestConstraintViolation(errorMessage, cast.ProductActivity, false) }, errorMessage);
        }

        [Fact]
        public void ConstraintViolatonInvalidExpression()
        {
            TestCast<string, int> cast = new TestCast<string, int> { Operand = "Hello" };

            string errorMessage = TestExpressionTracer.GetExceptionMessage<string, int>(System.Linq.Expressions.ExpressionType.Convert);

            TestExpressionTracer.Validate(cast, new List<string> { errorMessage });
        }

        public void Dispose()
        {
        }
    }
}
