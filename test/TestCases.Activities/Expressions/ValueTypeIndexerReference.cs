// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf.Statements;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace TestCases.Activities.Expressions
{
    public class ValueTypeIndexerReference
    {
        [Fact]
        public void SetValueOfAnItemInMultiDimensionalIndexer()
        {
            //  
            //  Test case description:
            //  Set value of an item in multi dimensional indexer.
            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            Variable<int> varIndex2 = new Variable<int>() { Default = 2, Name = "varIndex2" };
            Variable<int> varIndex3 = new Variable<int>() { Default = 3, Name = "varIndex3" };

            TestValueTypeIndexerReference<TheStruct, int> valueTypeIndexerReference = new TestValueTypeIndexerReference<TheStruct, int>()
            {
                OperandLocationVariable = var
            };

            valueTypeIndexerReference.Indices.Add(new TestArgument<int>(Direction.In, null, 1));
            valueTypeIndexerReference.Indices.Add(new TestArgument<int>(Direction.In, null, varIndex2));
            valueTypeIndexerReference.Indices.Add(new TestArgument<int>(Direction.In, null, varIndex3));

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeIndexerReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var, varIndex2, varIndex3 },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx)[1, 2, 3].ToString()), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void SetUninitializedIndices()
        {
            //  
            //  Test case description:
            //  Try executing ValueTypeIndexerReference activity without initializing indices. Validation exception
            //  expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeIndexerReference<TheStruct, int> valueTypeIndexerReference = new TestValueTypeIndexerReference<TheStruct, int>()
            {
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeIndexerReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx)[1].ToString()), HintMessage = value.ToString() }
                }
            };

            string error = string.Format(ErrorStrings.IndicesAreNeeded, valueTypeIndexerReference.ProductActivity.GetType().Name, valueTypeIndexerReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeIndexerReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }


        [Fact]
        public void TrySetItemNullOperand()
        {
            //  
            //  Test case description:
            //  Try executing ValueTypeIndexerReference activity without initializing indices. Validation exception
            //  expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeIndexerReference<TheStruct, int> valueTypeIndexerReference = new TestValueTypeIndexerReference<TheStruct, int>()
            {
                Indices =
                {
                    new TestArgument<int>(Direction.In, null, 2)
                }
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeIndexerReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "OperandLocation", valueTypeIndexerReference.ProductActivity.GetType().Name, valueTypeIndexerReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeIndexerReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void SetIndexWithCompatibleType()
        {
            //  
            //  Test case description:
            //  Try setting short indexed on integer indexer. 

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeIndexerReference<TheStruct, int> valueTypeIndexerReference = new TestValueTypeIndexerReference<TheStruct, int>()
            {
                OperandLocationVariable = var,
            };

            valueTypeIndexerReference.Indices.Add(new TestArgument<short>(Direction.In, null, 2));

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeIndexerReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx)[2].ToString()), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void ThrowFromIndexer()
        {
            //  
            //  Test case description:
            //  Throw from ‘this’ property

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeIndexerReference<TheStruct, int> valueTypeIndexerReference = new TestValueTypeIndexerReference<TheStruct, int>()
            {
                OperandLocationVariable = var,
            };

            valueTypeIndexerReference.Indices.Add(new TestArgument<float>(Direction.In, null, 2));

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypeIndexerReference,
                Value = value,
                ExpectedOutcome = Outcome.UncaughtException(typeof(Exception)),
            };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx)[2].ToString()), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateAbortedException(seq, typeof(Exception), null);
        }

        [Fact]
        public void TrySetTypeWithoutIndexer()
        {
            //  
            //  Test case description:
            //  Try setting indexer on type that does not support ‘this’ property. Validation exception is expected.

            int valueType = 0;

            Variable<int> var = new Variable<int>() { Default = valueType, Name = "var" };
            TestValueTypeIndexerReference<int, int> valueTypeIndexerReference = new TestValueTypeIndexerReference<int, int>()
            {
                OperandLocationVariable = var,
            };

            valueTypeIndexerReference.Indices.Add(new TestArgument<int>(Direction.In, null, 2));

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypeIndexerReference,
                Value = value,
            };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign
                }
            };

            string error = string.Format(ErrorStrings.SpecialMethodNotFound, "set_Item", typeof(int).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeIndexerReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySetReferenceTypeIndexer()
        {
            //  
            //  Test case description:
            //  Try setting indexer on reference type. Validation exception is expected. 

            Variable<TheClass> var = new Variable<TheClass>("var", context => new TheClass());
            TestValueTypeIndexerReference<TheClass, int> valueTypeIndexerReference = new TestValueTypeIndexerReference<TheClass, int>()
            {
                OperandLocationVariable = var,
            };

            valueTypeIndexerReference.Indices.Add(new TestArgument<int>(Direction.In, null, 2));

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypeIndexerReference,
                Value = value,
            };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign
                }
            };

            string error = string.Format(ErrorStrings.TypeMustbeValueType, typeof(TheClass).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeIndexerReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void SetDelegateArgument()
        {
            // for using delegate argument

            TheStruct valueType = new TheStruct();
            int indiceValue = 2;
            DelegateInArgument<int> indice = new DelegateInArgument<int>();
            Variable<TheStruct> var = VariableHelper.CreateInitialized<TheStruct>("var", valueType);
            TestValueTypeIndexerReference<TheStruct, int> valueTypeIndexerReference = new TestValueTypeIndexerReference<TheStruct, int>()
            {
                OperandLocationVariable = var,
            };

            valueTypeIndexerReference.Indices.Add(new TestArgument<int>(Direction.In, null, (env) => indice.Get(env)));

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeIndexerReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx)[indiceValue].ToString()) }
                }
            };

            Microsoft.CoreWf.Statements.Sequence outerSeq = new Microsoft.CoreWf.Statements.Sequence()
            {
                Variables =
                {
                    var
                },
                Activities =
                {
                    new InvokeAction<int>()
                    {
                        Argument = indiceValue,
                        Action = new ActivityAction<int>()
                        {
                            Argument = indice,
                            Handler = seq.ProductActivity
                        }
                    }
                }
            };

            TestCustomActivity testActivity = TestCustomActivity<Microsoft.CoreWf.Statements.Sequence>.CreateFromProduct(outerSeq);
            UnorderedTraces traces = new UnorderedTraces()
            {
                Steps =
                {
                    new UserTrace(value.ToString())
                }
            };

            testActivity.ActivitySpecificTraces.Add(traces);
            ExpectedTrace expectedTrace = testActivity.GetExpectedTrace();
            expectedTrace.AddIgnoreTypes(typeof(ActivityTrace));

            TestRuntime.RunAndValidateWorkflow(testActivity, expectedTrace);
        }
    }
}
