// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using System.Collections.Generic;
using Test.Common.TestObjects;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class NewArray : IDisposable
    {
        /// <summary>
        /// New an object of type with parameterless constructor.
        /// </summary>        
        [Fact]
        public void NewAnArrayOfReferenceType()
        {
            TestNewArray<PublicType[]> testNewArray = new TestNewArray<PublicType[]>();
            testNewArray.Bounds.Add(new InArgument<int>(3));

            Variable<PublicType[]> result = new Variable<PublicType[]>() { Name = "Result" };
            testNewArray.Result = result;

            TestSequence seq = new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    testNewArray,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = new PublicType[3].ToString() },
                    new TestWriteLine { MessageExpression = e => result.Get(e).Length.ToString(), HintMessage = "3" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// New an array of objects of value type.
        /// </summary>        
        [Fact]
        public void NewAnArrayOfValueType()
        {
            TestNewArray<int[]> testNewArray = new TestNewArray<int[]>();
            testNewArray.Bounds.Add(new InArgument<int>(3));

            Variable<int[]> result = new Variable<int[]>() { Name = "Result" };
            testNewArray.Result = result;

            TestSequence seq = new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    testNewArray,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = new int[3].ToString() },
                    new TestWriteLine { MessageExpression = e => result.Get(e).Length.ToString(), HintMessage = "3" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Should succeed
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentByte()
        {
            NewAnArrayWithTypeArgument<byte>(3);
        }

        /// <summary>
        /// Should succeed
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentLong()
        {
            TestNewArray<int[]> testNewArray = new TestNewArray<int[]>();
            testNewArray.Bounds.Add(new InArgument<long>(3));

            string error = string.Format(ErrorStrings.ConstructorInfoNotFound, typeof(int[]).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(error, testNewArray.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(testNewArray, constraints, error);
        }

        /// <summary>
        /// Should succeed
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentSByte()
        {
            NewAnArrayWithTypeArgument<sbyte>(3);
        }

        /// <summary>
        /// Should succeed
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentUInt()
        {
            TestNewArray<int[]> testNewArray = new TestNewArray<int[]>();
            testNewArray.Bounds.Add(new InArgument<uint>(3));

            string error = string.Format(ErrorStrings.ConstructorInfoNotFound, typeof(int[]).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(error, testNewArray.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(testNewArray, constraints, error);
        }

        /// <summary>
        /// Should succeed
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentULong()
        {
            TestNewArray<int[]> testNewArray = new TestNewArray<int[]>();
            testNewArray.Bounds.Add(new InArgument<ulong>(3));

            string error = string.Format(ErrorStrings.ConstructorInfoNotFound, typeof(int[]).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(error, testNewArray.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(testNewArray, constraints, error);
        }

        /// <summary>
        /// Should succeed
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentUShort()
        {
            NewAnArrayWithTypeArgument<ushort>(3);
        }

        /// <summary>
        /// New an array with type argument of type short.
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentShort()
        {
            NewAnArrayWithTypeArgument<short>(3);
        }

        /// <summary>
        /// Should succeed
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentChar()
        {
            TestNewArray<int[]> testNewArray = new TestNewArray<int[]>();
            testNewArray.Bounds.Add(new InArgument<char>('3'));

            Variable<int[]> result = new Variable<int[]>() { Name = "Result" };
            testNewArray.Result = result;

            TestSequence seq = new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    testNewArray,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = new int[(int)'3'].ToString() },
                    new TestWriteLine { MessageExpression = e => result.Get(e).Length.ToString(), HintMessage = ((int)'3').ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try newing an array with one of the arguments type not integral type. Validation exception.
        /// </summary>        
        [Fact]
        public void NewAnArrayWithTypeArgumentNotIntegral()
        {
            TestNewArray<int[]> testNewArray = new TestNewArray<int[]>();
            testNewArray.Bounds.Add(new InArgument<string>("3"));

            string error = ErrorStrings.NewArrayBoundsRequiresIntegralArguments;

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(error, testNewArray.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(testNewArray, constraints, error);
        }

        /// <summary>
        /// New a multi dimensional array.
        /// </summary>        
        [Fact]
        public void NewMultiDimensionalArray()
        {
            //TestParameters.DisableXamlRoundTrip = true;

            TestNewArray<int[,,]> multiDimArray = new TestNewArray<int[,,]>
            {
                Bounds =
                {
                    new InArgument<int>(2),
                    new InArgument<int>(3),
                    new InArgument<int>(5)
                }
            };

            Variable<int[,,]> result = new Variable<int[,,]>();
            multiDimArray.Result = result;

            TestSequence seq = new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    multiDimArray,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = new int[2,3,5].ToString() },
                    new TestWriteLine { MessageExpression = e => result.Get(e).Length.ToString(), HintMessage = "30" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// New a jagged array.
        /// </summary>        
        [Fact]
        public void NewJaggedArray()
        {
            //TestParameters.DisableXamlRoundTrip = true;
            TestNewArray<int[][]> jaggedArray = new TestNewArray<int[][]>
            {
                Bounds =
                {
                    new InArgument<int>(2),
                    new InArgument<int>(5)
                }
            };

            Variable<int[][]> result = new Variable<int[][]>();
            jaggedArray.Result = result;

            TestSequence seq = new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    jaggedArray,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = new int[2][].ToString() },
                    new TestWriteLine { MessageExpression = e => result.Get(e).Length.ToString(), HintMessage = "2" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// New an array with incorrect number of parameters.
        /// </summary>        
        [Fact]
        public void NewArrayWithIncorrectNumberOfParameters()
        {
            TestNewArray<int[]> intArray = new TestNewArray<int[]>
            {
                Bounds =
                {
                    new InArgument<int>(1),
                    new InArgument<int>(2)
                }
            };

            string error = string.Format(ErrorStrings.ConstructorInfoNotFound, typeof(int[]).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(error, intArray.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(intArray, constraints, error);
        }

        [Fact]
        public void NewArrayWithArrayType()
        {
            TestNewArray<Array> arr = new TestNewArray<Array>();

            string error = ErrorStrings.NewArrayRequiresArrayTypeAsResultType;

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(error, arr.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(arr, constraints, error);
        }

        /// <summary>
        /// Invoke with WorkflowInvoker
        /// </summary>        
        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestNewArray<int[]> newArray = new TestNewArray<int[]>()
            {
                Bounds =
                {
                    new InArgument<int>(3),
                },
            };

            IDictionary<string, object> result = WorkflowInvoker.Invoke(newArray.ProductActivity);
            int[] actualResult = (int[])result["Result"];

            if (actualResult.Length != 3)
                throw new Exception("Fail to create int[3]!");
        }

        private void NewAnArrayWithTypeArgument<T>(T constValue)
        {
            TestNewArray<int[]> testNewArray = new TestNewArray<int[]>();
            testNewArray.Bounds.Add(new InArgument<T>(constValue));

            Variable<int[]> result = new Variable<int[]>() { Name = "Result" };
            testNewArray.Result = result;

            TestSequence seq = new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    testNewArray,
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = new int[1].ToString() },
                    new TestWriteLine { MessageExpression = e => result.Get(e).Length.ToString(), HintMessage = constValue.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        public void Dispose()
        {
        }
    }
}
