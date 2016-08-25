// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace Test.TestCases.Activities.Expressions
{
    public class PropertyValue : IDisposable
    {
        /// <summary>
        /// Access a public property on an object.
        /// </summary>        
        [Fact]
        public void AccessPublicPropertyOnAnObject()
        {
            TestPropertyValue<PublicType, int> propertyValue = new TestPropertyValue<PublicType, int>
            {
                OperandExpression = context => new PublicType() { PublicProperty = 10 },
                PropertyName = "PublicProperty",
            };

            TestSequence seq = TestExpressionTracer.GetTraceablePropertyValue<PublicType, int>(propertyValue, "10");

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            TestRuntime.RunAndValidateUsingWorkflowInvoker(new TestPropertyValue<PublicType, int> { PropertyName = "PublicProperty" },
                                                           new Dictionary<string, object> { { "Operand", new PublicType() { PublicProperty = 22 } } },
                                                           new Dictionary<string, object> { { "Result", 22 } },
                                                           null);
        }

        /// <summary>
        /// Access a public property on a struct.
        /// </summary>        
        [Fact]
        public void AccessPublicPropertyOnAStruct()
        {
            TheStruct myStruct = new TheStruct { PublicProperty = 22 };

            TestPropertyValue<TheStruct, int> propValue = new TestPropertyValue<TheStruct, int> { Operand = myStruct, PropertyName = "PublicProperty" };

            TestSequence seq = TestExpressionTracer.GetTraceablePropertyValue<TheStruct, int>(propValue, myStruct.PublicProperty.ToString());

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Try accessing a property which does not have a getter. Exception expected.
        /// </summary>        
        [Fact]
        public void TryAccessingPropertyWithoutGetter()
        {
            PublicType myObj = new PublicType { WriteOnlyProperty = 1 };

            TestPropertyValue<PublicType, int> propertyValue = new TestPropertyValue<PublicType, int>(myObj, "WriteOnlyProperty");

            TestRuntime.ValidateInstantiationException(propertyValue, "");
        }

        /// <summary>
        /// Try accessing a field of an object. Validation exception.
        /// </summary>        
        [Fact]
        public void TryAccessingFieldNotProperty()
        {
            PublicType myObj = new PublicType { publicField = "10" };

            TestPropertyValue<PublicType, int> propertyValue = new TestPropertyValue<PublicType, int>(myObj, "publicField");

            TestRuntime.ValidateInstantiationException(propertyValue, "");
        }

        /// <summary>
        /// Access a public static property on an object.
        /// </summary>        
        [Fact]
        public void AccessStaticPropertyOnAnObject()
        {
            TestPropertyValue<PublicType, int> propertyValue = new TestPropertyValue<PublicType, int> { PropertyName = "StaticProperty" };
            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    new TestAssign<int>
                    {
                        ToLocation = new TestPropertyReference<PublicType, int> { PropertyName = "StaticProperty" },
                        Value = 10
                    },
                    TestExpressionTracer.GetTraceablePropertyValue<PublicType, int>(propertyValue, "10")
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void PassEnumTypeAsOperand()
        {
            WeekDay weekday = WeekDay.Monday;

            TestPropertyValue<WeekDay, int> propValue = new TestPropertyValue<WeekDay, int> { Operand = weekday, PropertyName = "Monday" };

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, propValue.ProductActivity.GetType().Name, propValue.DisplayName),
                propValue.ProductActivity));
            constraints.Add(new TestConstraintViolation(
                string.Format(ErrorStrings.MemberNotFound, "Monday", typeof(WeekDay).Name),
                propValue.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(propValue,
                constraints,
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, propValue.ProductActivity.GetType().Name, propValue.DisplayName));
        }

        [Fact]
        public void ConstraintErrorForPropertyNameNull()
        {
            TestPropertyValue<PublicType, int> propertyValue = new TestPropertyValue<PublicType, int>
            {
                OperandExpression = context => new PublicType()
            };

            TestExpressionTracer.Validate(propertyValue, new List<string> { string.Format(ErrorStrings.ActivityPropertyMustBeSet, "PropertyName", propertyValue.DisplayName) });
        }

        [Fact]
        public void ConstraintErrorForInvalidProperty()
        {
            TestPropertyValue<PublicType, int> propertyValue = new TestPropertyValue<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                PropertyName = "Invalid"
            };

            TestExpressionTracer.Validate(propertyValue, new List<string> { string.Format(ErrorStrings.MemberNotFound, "Invalid", typeof(PublicType).Name) });
        }

        [Fact]
        public void ConstraintErrorForEnumOperand()
        {
            WeekDay weekday = WeekDay.Monday;

            TestPropertyValue<WeekDay, int> propValue = new TestPropertyValue<WeekDay, int> { Operand = weekday, PropertyName = "Monday" };

            List<string> errors = new List<string>
            {
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, propValue.ProductActivity.GetType().Name, propValue.DisplayName),
                string.Format(ErrorStrings.MemberNotFound, "Monday", typeof(WeekDay).Name)
            };
            TestExpressionTracer.Validate(propValue, errors);
        }

        /// <summary>
        /// Try accessing a property which throws from get.
        /// </summary>        
        [Fact]
        public void ThrowExceptionFromGetterOFCustomTypeProperty()
        {
            TestPropertyValue<ExceptionThrowingSetterAndGetter, int> propertyValue = new TestPropertyValue<ExceptionThrowingSetterAndGetter, int>
            {
                OperandExpression = context => new ExceptionThrowingSetterAndGetter(),
                PropertyName = "ExceptionThrowingProperty",
                ExpectedOutcome = Outcome.UncaughtException()
            };

            TestRuntime.RunAndValidateAbortedException(propertyValue, typeof(IndexOutOfRangeException), null);
        }

        /// <summary>
        /// Try accessing a private property on an object. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TryAccessPrivatePropertyOnAnObject()
        {
            PublicType myObj = new PublicType();

            TestPropertyValue<PublicType, int> propertyValue = new TestPropertyValue<PublicType, int>(myObj, "PrivateProperty");

            TestRuntime.ValidateInstantiationException(propertyValue, string.Format(ErrorStrings.MemberNotFound, "PrivateProperty", typeof(PublicType).Name));
        }

        /// <summary>
        /// Try executing PropertyValue activity by setting PropertyName to null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TryGettingValueOfPropertyNameNull()
        {
            TestPropertyValue<PublicType, int> propertyValue = new TestPropertyValue<PublicType, int>
            {
                Operand = new PublicType()
            };

            TestRuntime.ValidateInstantiationException(propertyValue, string.Format(ErrorStrings.ActivityPropertyMustBeSet, "PropertyName", propertyValue.DisplayName));
        }

        public void Dispose()
        {
        }
    }
}
