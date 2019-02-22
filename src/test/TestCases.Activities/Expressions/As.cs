// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Runtime;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class As : IDisposable
    {
        [Fact]
        public void UseBaseTypeAsDerivedType()
        {
            Variable<Derived> result = new Variable<Derived>() { Name = "Result" };
            Variable<string> output = new Variable<string>() { Name = "Output" };

            TestInvokeMethod invokeMethod = new TestInvokeMethod
            {
                TargetObjectVariable = result,
                MethodName = "MethodInDerivedType"
            };

            invokeMethod.SetResultVariable<string>(output);

            //Base b = new Derived();
            //string result = (b as Derived).MethodInDerivedType
            TestSequence seq = new TestSequence
            {
                Variables = { result, output },
                Activities =
                {
                    new TestAs<Base, Derived>
                    {
                        OperandExpression = context => new Derived(),
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

        /// <summary>
        /// Try converting an int to string using as operator. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TryConvertingIntToString()
        {
            Variable<string> result = new Variable<string>() { Name = "Result" };
            TestAs<int, string> asExp = new TestAs<int, string> { Operand = 3, Result = result };


            TestSequence seq = new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    asExp,
                    new TestIf
                    {
                        ConditionExpression = (env => result.Get(env) == null),
                        ThenActivity = new TestWriteLine("Done", "Done"),
                        ElseActivity = new TestThrow<Exception>()
                    }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestAs<Base, Derived>(),
                                                           new Dictionary<string, object> { { "Operand", new Derived() } },
                                                           new Dictionary<string, object> { { "Result", new Derived() } },
                                                           null);
        }

        /// <summary>
        /// Make sure that As returns null if conversion of two types is not possible.
        /// </summary>        
        //[Fact]
        //public void TryAsOnUnrelatedTypes()
        //{
        //    Variable<Derived> result = new Variable<Derived>("Result", context => new Derived());

        //    TestSequence seq = new TestSequence
        //    {
        //        Variables = { result },
        //        Activities =
        //        {
        //            new TestAs<string, Derived> { Operand = "xxx", Result = result },
        //            new TestIf(new HintThenOrElse[] { HintThenOrElse.Then })
        //            {
        //                ConditionExpression = e => result.Get(e) == null,
        //                ThenActivity = new TestWriteLine("Done", "Done"),
        //                ElseActivity = new TestThrow<Exception>()
        //                { 
        //                    ExceptionExpression = (context => new Exception("Null was exppected."))
        //                },
        //            }
        //        }
        //    };

        //    VisualBasicUtility.AttachVisualBasicSettingsProperty(seq.ProductActivity, new List<Type>() { typeof(TestCaseException) });
        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        public void Dispose()
        {
        }
    }
}
