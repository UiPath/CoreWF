// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class New : IDisposable
    {
        /// <summary>
        /// New an object of type with parameterless constructor.
        /// New an object of type with parameter less constructor
        /// </summary>        
        [Fact]
        public void ConstructAnObjectWithParameterLessConstructor()
        {
            TestNew<ParameterLessConstructorType> myNew = new TestNew<ParameterLessConstructorType>();

            TestSequence seq = GetTraceableTestNew<ParameterLessConstructorType>(myNew, new ParameterLessConstructorType().ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }



        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestNew<ParameterLessConstructorType>(),
                                                           null,
                                                           new Dictionary<string, object> { { "Result", new ParameterLessConstructorType() } },
                                                           null);
        }

        /// <summary>
        /// Construct an object of struct type.
        /// </summary>        
        [Fact]
        public void ConstructAnObjectOfStruct()
        {
            TestNew<TheStruct> myNew = new TestNew<TheStruct>();
            myNew.Arguments.Add(new TestArgument<int>(Direction.In, "Number", 4));

            TestSequence seq = GetTraceableTestNew<TheStruct>(myNew, new TheStruct(4).ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Construct an object of type by passing null values for all parameters in constructor.
        /// </summary>
        //[Fact]
        //public void ConstructAnObjectPassingNullValuesToParameters()
        //{
        //    TestNew<ParameteredConstructorType> myNew = new TestNew<ParameteredConstructorType>();
        //    myNew.Arguments.Add(new TestArgument<Complex>(Direction.In, "Parameter", (Complex)null));

        //    TestSequence seq = GetTraceableTestNew<ParameteredConstructorType>(myNew, "null");

        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        /// <summary>
        /// Try constructing an object of type which has no parameter less constructor and do not provide any parameters. Validation exception expected.
        /// </summary>        
        [Fact]
        public void ConstructAnObjectWithTypeHavingNoParameterLessConstructorWithoutProvidingTypes()
        {
            TestNew<ParameteredConstructorType> myNew = new TestNew<ParameteredConstructorType>();

            TestSequence seq = GetTraceableTestNew<ParameteredConstructorType>(myNew, "null");

            string error = string.Format(ErrorStrings.ConstructorInfoNotFound, typeof(ParameteredConstructorType).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(error, myNew.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        /// <summary>
        /// Construct a type which takes an out parameter in constructor.
        /// </summary>        
        [Fact]
        public void ConstructAnObjectWithTypeWhichTakesAnOutParameterInConstructor()
        {
            Variable<int> outVariable = new Variable<int>() { Name = "OutVar" };

            TestNew<TypeWithOutParameterInConstructor> mine = new TestNew<TypeWithOutParameterInConstructor>();
            mine.Arguments.Add(new TestArgument<int>(Direction.Out, "OutArg", outVariable));

            TestSequence seq = new TestSequence
            {
                Variables = { outVariable },
                Activities =
                {
                    mine,
                    new TestWriteLine { MessageExpression = e => outVariable.Get(e).ToString(), HintMessage = "10" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Construct a type which takes a ref parameter in constructor.
        /// </summary>        
        [Fact]
        public void ConstructAnObjectWithTypeWhichTakesARefParameterInConstructor()
        {
            Variable<int> outVariable = new Variable<int>() { Name = "OutVar" };

            TestNew<TypeWithRefParameterInConstructor> mine = new TestNew<TypeWithRefParameterInConstructor>();
            mine.Arguments.Add(new TestArgument<int>(Direction.InOut, "OutArg", outVariable));

            TestSequence seq = new TestSequence
            {
                Variables = { outVariable },
                Activities =
                {
                    mine,
                    new TestWriteLine { MessageExpression = e => outVariable.Get(e).ToString(), HintMessage = "10" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try constructing an object of value type (enum).
        /// </summary>        
        [Fact]
        public void ConstructAnObjectOfValueType()
        {
            TestNew<WeekDay> myNew = new TestNew<WeekDay>();

            TestSequence seq = GetTraceableTestNew<WeekDay>(myNew, new WeekDay().ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Construct a type and provide less parameters than expected by constructor. Validation exception
        /// </summary>        
        [Fact]
        public void ConstructObjectByProvidingLessParameters()
        {
            TestNew<Complex> myNew = new TestNew<Complex>();
            myNew.Arguments.Add(new TestArgument<int>(Direction.In, "Real", 1));

            string error = string.Format(ErrorStrings.ConstructorInfoNotFound, typeof(Complex).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(error, myNew.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(myNew, constraints, error);
        }

        /// <summary>
        /// Construct a type and provide more parameters than expected by constructor. Validation exception
        /// </summary>        
        [Fact]
        public void ConstructObjectByProvidingMoreParameters()
        {
            TestNew<Complex> myNew = new TestNew<Complex>();
            myNew.Arguments.Add(new TestArgument<int>(Direction.In, "Real", 1));
            myNew.Arguments.Add(new TestArgument<int>(Direction.In, "Imaginary", 1));
            myNew.Arguments.Add(new TestArgument<int>(Direction.In, "Complex", 1));

            string error = string.Format(ErrorStrings.ConstructorInfoNotFound, typeof(Complex).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(error, myNew.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(myNew, constraints, error);
        }

        /// <summary>
        /// New an object of custom type which takes parameters of custom types.
        /// </summary>        
        [Fact]
        public void ConstructAnObjectOfCustomTypeHavingCustomTypeParameters()
        {
            TestNew<ParameteredConstructorType> myNew = new TestNew<ParameteredConstructorType>();
            myNew.Arguments.Add(new TestArgument<Complex>(Direction.In, "Parameter", context => new Complex(1, 2)));

            TestSequence seq = GetTraceableTestNew<ParameteredConstructorType>(myNew, new Complex(1, 2).ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void ConstraintErrorForInvalidArguments()
        {
            TestNew<Complex> myNew = new TestNew<Complex>();
            myNew.Arguments.Add(new TestArgument<int>(Direction.In, "Real", 1));

            string error = string.Format(ErrorStrings.ConstructorInfoNotFound, typeof(Complex).Name);

            TestExpressionTracer.Validate(myNew, new List<string> { error });
        }

        /// <summary>
        /// Construct a type whose constructor takes in, out, ref parameters which are interlaced.
        /// </summary>        
        [Fact]
        public void ConstructAnObjectWithTypeWhoseConstructorHasInOutRefInterlacedParams()
        {
            Variable<int> inVariable1 = new Variable<int>() { Name = "InVar1" };
            Variable<int> inVariable2 = new Variable<int>() { Name = "InVar2" };
            Variable<int> outVariable1 = new Variable<int>() { Name = "OutVar1" };
            Variable<int> outVariable2 = new Variable<int>() { Name = "OutVar2" };
            Variable<int> refVariable1 = new Variable<int>() { Name = "RefVar1" };
            Variable<int> refVariable2 = new Variable<int>() { Name = "RefVar2" };

            TestNew<TypeWithRefAndOutParametersinConstructor> mine = new TestNew<TypeWithRefAndOutParametersinConstructor>();
            mine.Arguments.Add(new TestArgument<int>(Direction.In, "InArg1", inVariable1));
            mine.Arguments.Add(new TestArgument<int>(Direction.Out, "OutArg1", outVariable1));
            mine.Arguments.Add(new TestArgument<int>(Direction.In, "InArg2", inVariable1));
            mine.Arguments.Add(new TestArgument<int>(Direction.InOut, "RefArg1", refVariable1));
            mine.Arguments.Add(new TestArgument<int>(Direction.InOut, "RefArg2", refVariable2));
            mine.Arguments.Add(new TestArgument<int>(Direction.Out, "OutArg2", outVariable2));

            TestSequence seq = new TestSequence
            {
                Variables = { inVariable1, inVariable2, outVariable1, outVariable2, refVariable1, refVariable2 },
                Activities =
                {
                    mine,
                    new TestWriteLine { MessageExpression = e => outVariable1.Get(e).ToString(), HintMessage = "13" },
                    new TestWriteLine { MessageExpression = e => outVariable2.Get(e).ToString(), HintMessage = "13" },
                    new TestWriteLine { MessageExpression = e => refVariable1.Get(e).ToString(), HintMessage = "13" },
                    new TestWriteLine { MessageExpression = e => refVariable2.Get(e).ToString(), HintMessage = "13" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        private TestSequence GetTraceableTestNew<T>(TestNew<T> testNew, string expectedResult)
        {
            Variable<T> result = new Variable<T>() { Name = "Result" };

            testNew.Result = result;

            return new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    testNew,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = expectedResult }
                }
            };
        }

        public void Dispose()
        {
        }
    }
}
