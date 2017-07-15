// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
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
    public class FieldReference : IDisposable
    {
        /// <summary>
        /// Set a public property on an object.
        /// </summary>        
        [Fact]
        public void SetPublicFieldOnAnObject()
        {
            Variable<PublicType> customType = new Variable<PublicType>("Custom", context => new PublicType() { publicField = "public" });

            TestFieldReference<PublicType, string> fieldReference = new TestFieldReference<PublicType, string> { OperandVariable = customType, FieldName = "publicField" };

            TestSequence seq = new TestSequence
            {
                Variables = { customType },
                Activities =
                {
                    new TestAssign<string> { ToLocation = fieldReference, Value = "private" },
                    new TestWriteLine { MessageExpression = e => customType.Get(e).publicField, HintMessage = "private" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Set a public static field on an object.
        /// </summary>        
        [Fact]
        public void SetStaticFieldOnAnObject()
        {
            TestFieldReference<PublicType, int> fieldReference = new TestFieldReference<PublicType, int> { FieldName = "staticField" };

            TestSequence seq = new TestSequence
            {
                Activities =
                {
                    new TestAssign<int> { ToLocation = fieldReference, Value = 22 },
                    new TestWriteLine { MessageExpression = e => PublicType.staticField.ToString(), HintMessage = "22" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Set a public field on a struct object.
        /// </summary>        
        [Fact]
        public void SetPublicFieldOnAStruct()
        {
            //TheStruct.publicField wont be serialized so round tripping loses the value.
            Variable<TheStruct> customType = new Variable<TheStruct>("CustomType", context => new TheStruct { publicField = 1 });

            TestFieldReference<TheStruct, int> fieldReference = new TestFieldReference<TheStruct, int>
            {
                OperandVariable = customType,
                FieldName = "publicField"
            };

            TestSequence seq = new TestSequence
            {
                Variables = { customType },
                Activities =
                {
                    new TestAssign<int> { ToLocation = fieldReference, Value = 1793 },
                    new TestWriteLine { MessageExpression = e => customType.Get(e).publicField.ToString(), HintMessage = "0" }
                }
            };

            string error = string.Format(ErrorStrings.TargetTypeIsValueType, typeof(FieldReference<TheStruct, int>).Name, fieldReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                error,
                fieldReference.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void PassEnumTypeAsOperand()
        {
            Variable<WeekDay> weekDayVariable = new Variable<WeekDay>("weekDayVariable");
            TestFieldReference<WeekDay, int> fieldRef = new TestFieldReference<WeekDay, int>
            {
                OperandVariable = weekDayVariable,
                FieldName = "Monday"
            };
            TestSequence testSequence = new TestSequence
            {
                Variables =
                {
                    weekDayVariable,
                },
                Activities =
                {
                    new TestAssign<WeekDay> { ToVariable = weekDayVariable, ValueExpression = (context => WeekDay.Monday) },
                    fieldRef,
                }
            };

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, typeof(FieldReference<WeekDay, int>).Name, fieldRef.DisplayName),
                fieldRef.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(testSequence, constraints, string.Format(ErrorStrings.TargetTypeCannotBeEnum, typeof(FieldReference<WeekDay, int>).Name, fieldRef.DisplayName));
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            Dictionary<string, object> results = WorkflowInvoker.Invoke((Activity)new FieldReference<PublicType, int>() { FieldName = "publicField" },
                                                                        new Dictionary<string, object> { { "Operand", new PublicType() } }) as Dictionary<string, object>;

            if (results["Result"] == null)
            {
                throw new Exception("Result was expected to be in output");
            }
        }

        /// <summary>
        /// Set a public static field on a struct.
        /// </summary>        
        [Fact]
        public void SetStaticFieldOnAStruct()
        {
            TestFieldReference<TheStruct, int> fieldReference = new TestFieldReference<TheStruct, int> { FieldName = "staticField" };

            TestSequence seq = new TestSequence
            {
                Activities =
                {
                    new TestAssign<int> { ToLocation = fieldReference, Value = 22 },
                    new TestWriteLine { MessageExpression = e => TheStruct.staticField.ToString(), HintMessage = "22" }
                }
            };

            string error = string.Format(ErrorStrings.TargetTypeIsValueType, typeof(FieldReference<TheStruct, int>).Name, fieldReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                error,
                fieldReference.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        /// <summary>
        /// Try setting a private field on an object. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TrySettingPrivateFieldOnAnObject()
        {
            TestFieldReference<PublicType, int> fieldReference = new TestFieldReference<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                FieldName = "privateField"
            };

            string error = string.Format(ErrorStrings.MemberNotFound, "privateField", typeof(PublicType).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    fieldReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(fieldReference, constraints, error);
        }

        [Fact]
        public void ConstraintErrorForInvalidField()
        {
            TestFieldReference<PublicType, int> fieldRef = new TestFieldReference<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                FieldName = "!@$!@#%><?<?<*&^(*&^("
            };

            string error = string.Format(ErrorStrings.MemberNotFound, "!@$!@#%><?<?<*&^(*&^(", typeof(PublicType).Name);

            TestExpressionTracer.Validate(fieldRef, new List<string> { error });
        }

        [Fact]
        public void ConstraintErrorForFieldNameNull()
        {
            TestFieldReference<PublicType, int> fieldRef = new TestFieldReference<PublicType, int> { OperandExpression = context => new PublicType() };

            string error = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "FieldName", fieldRef.DisplayName);

            TestExpressionTracer.Validate(fieldRef, new List<string> { error });
        }

        [Fact]
        public void ConstraintErrorForEnumOperand()
        {
            TestFieldReference<WeekDay, int> fieldRef = new TestFieldReference<WeekDay, int>
            {
                Operand = WeekDay.Monday,
                FieldName = "Monday"
            };

            List<string> errors = new List<string>
            {
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, typeof(FieldReference<WeekDay, int>).Name, fieldRef.DisplayName)
            };

            TestExpressionTracer.Validate(fieldRef, errors);
        }

        [Fact]
        public void ConstraintErrorForValueTypeOperand()
        {
            TestFieldReference<TheStruct, int> fieldRef = new TestFieldReference<TheStruct, int>
            {
                Operand = new TheStruct(),
                FieldName = "publicField"
            };

            string error = string.Format(ErrorStrings.TargetTypeIsValueType, typeof(FieldReference<TheStruct, int>).Name, fieldRef.DisplayName);

            TestExpressionTracer.Validate(fieldRef, new List<string> { error });
        }

        /// <summary>
        /// Try executing FieldReference activity by setting FieldName to null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TrySettingValueOfFieldNameNull()
        {
            Variable<PublicType> publicTypeVariable = new Variable<PublicType>("publicTypeVariable");
            TestFieldReference<PublicType, int> fieldRef = new TestFieldReference<PublicType, int> { OperandVariable = publicTypeVariable };
            TestSequence testSequence = new TestSequence
            {
                Variables =
                {
                    publicTypeVariable,
                },
                Activities =
                {
                    new TestAssign<PublicType> { ToVariable = publicTypeVariable, ValueExpression = (context => new PublicType()) },
                    fieldRef
                }
            };

            string exceptionString = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "FieldName", fieldRef.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    exceptionString,
                    fieldRef.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(testSequence, constraints, exceptionString);
        }

        public void Dispose()
        {
        }
    }
}
