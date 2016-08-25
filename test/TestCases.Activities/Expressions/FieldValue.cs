// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
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
    public class FieldValue : IDisposable
    {
        /// <summary>
        /// Access a public field on an object.
        /// </summary>        
        [Fact]
        public void AccessPublicFieldOnAnObject()
        {
            Variable<PublicType> publicTypeVariable = new Variable<PublicType>("publicTypeVariable");
            Variable<string> result = new Variable<string>() { Name = "Result" };

            TestFieldReference<PublicType, string> fieldReference = new TestFieldReference<PublicType, string> { OperandVariable = publicTypeVariable, FieldName = "publicField" };

            TestSequence seq = new TestSequence
            {
                Variables = { publicTypeVariable, result },
                Activities =
                {
                    new TestAssign<PublicType> { ToVariable = publicTypeVariable, ValueExpression = (context => new PublicType()) },
                    new TestAssign<string> { ToLocation = fieldReference, Value = "CheckCheck123" },
                    new TestFieldValue<PublicType, string> { OperandVariable = publicTypeVariable, FieldName = "publicField", Result = result },
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = "CheckCheck123" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Access a static field on a type.
        /// Access a public static field on an object.
        /// </summary>        
        [Fact]
        public void AccessStaticFieldOnAnObject()
        {
            TestFieldReference<PublicType, int> fieldReference = new TestFieldReference<PublicType, int> { FieldName = "staticField" };

            Variable<int> result = new Variable<int>() { Name = "result" };
            TestSequence seq = new TestSequence
            {
                Variables = { result },
                Activities =
                {
                    new TestAssign<int> { ToLocation = fieldReference, Value = 22 },
                    new TestFieldValue<PublicType, int> { FieldName = "staticField", Result= result },
                    new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = "22" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Access a public field on an struct.
        /// </summary>        
        //[Fact]
        //public void AccessPublicFieldOnAStruct()
        //{
        //    TestCase.Current.Parameters.Add("DisableXamlRoundTrip", "true");

        //    TheStruct myStruct = new TheStruct(10);

        //    Variable<TheStruct> structVar = new Variable<TheStruct>() { Default = myStruct };
        //    Variable<int> result = new Variable<int>();

        //    TestSequence seq = new TestSequence
        //    {
        //        Variables = { structVar, result },
        //        Activities =
        //        {
        //            new TestFieldValue<TheStruct, int>{ OperandVariable = structVar, FieldName = "publicField", Result = result },
        //            new TestWriteLine { MessageExpression = e => result.Get(e).ToString(), HintMessage = "10" }
        //        }
        //    };

        //    TestRuntime.RunAndValidateWorkflow(seq);
        //}

        [Fact]
        public void PassEnumTypeAsOperand()
        {
            TestFieldValue<WeekDay, WeekDay> field = new TestFieldValue<WeekDay, WeekDay>
            {
                Operand = WeekDay.Monday,
                FieldName = "Monday"
            };

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, field.ProductActivity.GetType().Name, field.DisplayName),
                field.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(field,
                constraints,
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, field.ProductActivity.GetType().Name, field.ProductActivity.DisplayName));
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestFieldValue<PublicType, string>() { FieldName = "publicField" },
                                                           new Dictionary<string, object> { { "Operand", new PublicType { publicField = "10" } } },
                                                           new Dictionary<string, object> { { "Result", "10" } },
                                                           null);
        }

        /// <summary>
        /// Try accessing a private field on an object. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TryAccessPrivateFieldOnAnObject()
        {
            TestFieldValue<PublicType, int> fieldVal = new TestFieldValue<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                FieldName = "privateField",
            };

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                string.Format(ErrorStrings.MemberNotFound, "privateField", typeof(PublicType).Name),
                fieldVal.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(fieldVal, constraints, string.Format(ErrorStrings.MemberNotFound, "privateField", typeof(PublicType).Name));
        }

        /// <summary>
        /// Try executing FieldValue activity by setting FieldName to null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TryGettingValueOfFieldNameNull()
        {
            TestFieldValue<PublicType, int> fieldValue = new TestFieldValue<PublicType, int>
            {
                OperandExpression = context => new PublicType()
            };

            string error = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "FieldName", fieldValue.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                error,
                fieldValue.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(fieldValue, constraints, error);
        }

        /// <summary>
        /// Try accessing a property of an object by FieldValue activity. Validation exception.
        /// </summary>        
        [Fact]
        public void TryAccessingPropertyNotField()
        {
            TestFieldValue<PublicType, int> fieldVal = new TestFieldValue<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                FieldName = "PublicProperty",
            };

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                string.Format(ErrorStrings.MemberNotFound, "PublicProperty", typeof(PublicType).Name),
                fieldVal.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(fieldVal, constraints, string.Format(ErrorStrings.MemberNotFound, "PublicProperty", typeof(PublicType).Name));
        }

        [Fact]
        public void ConstraintErrorForEnumOperand()
        {
            TestFieldValue<WeekDay, WeekDay> field = new TestFieldValue<WeekDay, WeekDay>
            {
                Operand = WeekDay.Monday,
                FieldName = "Monday"
            };

            string error = string.Format(ErrorStrings.TargetTypeCannotBeEnum, field.ProductActivity.GetType().Name, field.DisplayName);

            TestExpressionTracer.Validate(field, new List<string> { error });
        }

        [Fact]
        public void ConstraintErrorForFieldNameNull()
        {
            TestFieldValue<PublicType, int> fieldValue = new TestFieldValue<PublicType, int>
            {
                OperandExpression = context => new PublicType()
            };

            string error = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "FieldName", fieldValue.DisplayName);

            TestExpressionTracer.Validate(fieldValue, new List<string> { error });
        }

        [Fact]
        public void ConstraintErrorForInvalidField()
        {
            TestFieldValue<PublicType, int> fieldVal = new TestFieldValue<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                FieldName = "PublicProperty",
            };

            string error = string.Format(ErrorStrings.MemberNotFound, "PublicProperty", typeof(PublicType).Name);

            TestExpressionTracer.Validate(fieldVal, new List<string> { error });
        }

        public void Dispose()
        {
        }
    }
}
