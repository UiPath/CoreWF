// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
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
    public class PropertyReference : IDisposable
    {
        /// <summary>
        /// Set a public property on an object.
        /// </summary>        
        [Fact]
        public void SetPublicPropertyOnAnObject()
        {
            Variable<System.Activities.Statements.Sequence> customType = new Variable<System.Activities.Statements.Sequence>() { Name = "Custom" };

            TestPropertyReference<System.Activities.Statements.Sequence, string> propReference = new TestPropertyReference<System.Activities.Statements.Sequence, string> { OperandVariable = customType, PropertyName = "DisplayName" };

            TestSequence seq = new TestSequence
            {
                Variables = { customType },
                Activities =
                {
                    new TestAssign<System.Activities.Statements.Sequence> { ToVariable = customType, ValueExpression = (context => new System.Activities.Statements.Sequence() { DisplayName = "MySequence" }) },
                    new TestAssign<string> { ToLocation = propReference, Value = "NotMySequence" },
                    new TestWriteLine { MessageExpression = e => customType.Get(e).DisplayName, HintMessage = "NotMySequence" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            PublicType myType = new PublicType();

            Dictionary<string, object> results = WorkflowInvoker.Invoke((Activity)new PropertyReference<PublicType, int>() { PropertyName = "PublicProperty" },
                                                                        new Dictionary<string, object> { { "Operand", myType } }) as Dictionary<string, object>;

            if (results["Result"] == null)
            {
                throw new Exception("Result was expected to be in output");
            }
        }

        [Fact]
        public void PassEnumTypeAsOperand()
        {
            TestPropertyReference<WeekDay, int> propertyRef = new TestPropertyReference<WeekDay, int>
            {
                Operand = WeekDay.Monday,
                PropertyName = "Monday"
            };

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    string.Format(ErrorStrings.TargetTypeCannotBeEnum, propertyRef.ProductActivity.GetType().Name, propertyRef.DisplayName),
                    propertyRef.ProductActivity),
                new TestConstraintViolation(
                    string.Format(ErrorStrings.MemberNotFound, "Monday", typeof(WeekDay).Name),
                    propertyRef.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(
                propertyRef,
                constraints,
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, propertyRef.ProductActivity.GetType().Name, propertyRef.DisplayName));
        }

        /// <summary>
        /// Set a public property on a struct object.
        /// </summary>        
        [Fact]
        public void SetPublicPropertyOnAStruct()
        {
            TheStruct myStruct = new TheStruct { PublicProperty = 23 };
            Variable<TheStruct> customType = new Variable<TheStruct>() { Name = "Custom", Default = myStruct };

            TestPropertyReference<TheStruct, int> propReference = new TestPropertyReference<TheStruct, int>
            {
                OperandVariable = customType,
                PropertyName = "PublicProperty"
            };

            TestSequence seq = new TestSequence
            {
                Variables = { customType },
                Activities =
                {
                    new TestAssign<int> { ToLocation = propReference, Value = 27 },
                    new TestWriteLine { MessageExpression = e => customType.Get(e).publicField.ToString(), HintMessage = "0" }
                }
            };

            string error = string.Format(ErrorStrings.TargetTypeIsValueType, typeof(PropertyReference<TheStruct, int>).Name, propReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                error,
                propReference.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        /// <summary>
        /// Set a public static property on an object.
        /// </summary>        
        [Fact]
        public void SetStaticPropertyOnAnObject()
        {
            TestPropertyReference<PublicType, int> propReference = new TestPropertyReference<PublicType, int> { PropertyName = "StaticProperty" };

            TestSequence seq = new TestSequence
            {
                Activities =
                {
                    new TestAssign<int> { ToLocation = propReference, Value = 110 },
                    new TestWriteLine { MessageExpression = e => PublicType.StaticProperty.ToString(), HintMessage = "110" }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void ConstraintErrorForNullPropertyName()
        {
            TestPropertyReference<PublicType, int> propertyRef = new TestPropertyReference<PublicType, int>
            {
                OperandExpression = context => new PublicType()
            };

            TestExpressionTracer.Validate(propertyRef, new List<string> { string.Format(ErrorStrings.ActivityPropertyMustBeSet, "PropertyName", propertyRef.DisplayName) });
        }

        [Fact]
        public void ConstraintErrorForInvalidProperty()
        {
            TestPropertyReference<PublicType, int> propertyRef = new TestPropertyReference<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                PropertyName = "Invalid"
            };

            TestExpressionTracer.Validate(propertyRef, new List<string> { string.Format(ErrorStrings.MemberNotFound, "Invalid", typeof(PublicType).Name) });
        }

        [Fact]
        public void ConstraintErrorForEnumOperand()
        {
            WeekDay weekday = WeekDay.Monday;

            TestPropertyReference<WeekDay, int> propRef = new TestPropertyReference<WeekDay, int> { Operand = weekday, PropertyName = "Monday" };

            List<string> errors = new List<string>
            {
                string.Format(ErrorStrings.TargetTypeCannotBeEnum, propRef.ProductActivity.GetType().Name, propRef.DisplayName),
                string.Format(ErrorStrings.MemberNotFound, "Monday", typeof(WeekDay).Name)
            };
            TestExpressionTracer.Validate(propRef, errors);
        }

        /// <summary>
        /// Try executing PropertyReference activity by setting PropertyName to null. Validation exception expected.
        /// </summary>        
        [Fact]
        public void PropertyNameNull()
        {
            TestPropertyReference<PublicType, int> propRef = new TestPropertyReference<PublicType, int>
            {
                Operand = new PublicType()
            };

            TestRuntime.ValidateInstantiationException(propRef, string.Format(ErrorStrings.ActivityPropertyMustBeSet, "PropertyName", propRef.DisplayName));
        }

        /// <summary>
        /// Try setting a property which throws from setter.
        /// </summary>        
        [Fact]
        public void ThrowExceptionFromSetterOFCustomTypeProperty()
        {
            Variable<ExceptionThrowingSetterAndGetter> customType = new Variable<ExceptionThrowingSetterAndGetter>("Custom", context => new ExceptionThrowingSetterAndGetter());

            TestPropertyReference<ExceptionThrowingSetterAndGetter, int> propReference = new TestPropertyReference<ExceptionThrowingSetterAndGetter, int>
            {
                OperandVariable = customType,
                PropertyName = "ExceptionThrowingProperty",
            };

            TestSequence seq = new TestSequence
            {
                Variables = { customType },
                Activities =
                {
                    new TestAssign<int> { ToLocation = propReference, Value = 10, ExpectedOutcome = Outcome.UncaughtException() },
                }
            };

            TestRuntime.RunAndValidateAbortedException(seq, typeof(IndexOutOfRangeException), null);
        }

        /// <summary>
        /// Try setting a field of an object. Validation exception.
        /// </summary>        
        [Fact]
        public void TrySettingFieldNotProperty()
        {
            TestPropertyReference<PublicType, int> propReference = new TestPropertyReference<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                PropertyName = "publicField",
            };

            TrySettingInvalidProperty(propReference, "publicField");
        }

        /// <summary>
        /// Try setting a private property on an object. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TrySettingPrivatePropertyOnAnObject()
        {
            TestPropertyReference<PublicType, int> propReference = new TestPropertyReference<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                PropertyName = "PrivateProperty",
            };

            TrySettingInvalidProperty(propReference, "PrivateProperty");
        }

        /// <summary>
        /// Try setting value of a property in a type which does not exist. Validation exception expected.
        /// </summary>        
        [Fact]
        public void TrySettingPropertyOfNonExistentProperty()
        {
            TestPropertyReference<PublicType, int> propReference = new TestPropertyReference<PublicType, int>
            {
                OperandExpression = context => new PublicType(),
                PropertyName = "NonExistent",
            };

            TrySettingInvalidProperty(propReference, "NonExistent");
        }

        private void TrySettingInvalidProperty(TestPropertyReference<PublicType, int> propRef, string propName)
        {
            TestAssign<int> assign = new TestAssign<int> { ToLocation = propRef, Value = 10, ExpectedOutcome = Outcome.UncaughtException() };

            string error = string.Format(ErrorStrings.MemberNotFound, propName, typeof(PublicType).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>();
            constraints.Add(new TestConstraintViolation(
                error,
                propRef.ProductActivity));

            TestRuntime.ValidateWorkflowErrors(assign, constraints, error);
        }

        public void Dispose()
        {
        }
    }
}
