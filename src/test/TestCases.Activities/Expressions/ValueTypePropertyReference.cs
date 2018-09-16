// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Collections.Generic;
using System.IO;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace TestCases.Activities.Expressions
{
    public class ValueTypePropertyReference
    {
        [Fact]
        public void SetPublicPropertyOnValueType()
        {
            //  
            //  Test case description:
            //  Set a public property on value type.

            TheStruct valueType = new TheStruct
            {
                PublicProperty = 123
            };

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
            {
                PropertyName = "PublicProperty",
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypePropertyReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx).PublicProperty.ToString()), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void SetPublicEnumPropertyOnValueType()
        {
            //  
            //  Test case description:
            //  Set a public enum property on value type.
            TheStruct valueType = new TheStruct
            {
                EnumProperty = FileAccess.Write
            };

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypePropertyReference<TheStruct, FileAccess> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, FileAccess>()
            {
                PropertyName = "EnumProperty",
                OperandLocationVariable = var,
            };

            FileAccess value = System.IO.FileAccess.ReadWrite;
            TestAssign<FileAccess> testAssign = new TestAssign<FileAccess>() { ToLocation = valueTypePropertyReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx).EnumProperty.ToString()), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void SetStaticPropertyOnValueType()
        {
            //  
            //  Test case description:
            //  Set a public static property on value type.

            TheStruct.StaticStringProperty = "original value";

            TestValueTypePropertyReference<TheStruct, string> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, string>()
            {
                PropertyName = "StaticStringProperty",
            };

            string value = "new value";
            TestAssign<string> testAssign = new TestAssign<string>() { ToLocation = valueTypePropertyReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => TheStruct.StaticStringProperty), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        //[Fact]
        //public void TrySettingPrivatePropertyOnValueType()
        //{
        //    //  
        //    //  Test case description:
        //    //  Try setting a private property on value type. Validation exception expected.

        //    TheStruct valueType = new TheStruct();

        //    Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
        //    TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
        //    {
        //        PropertyName = "PrivateProperty",
        //        OperandLocation = new TestVisualBasicReference<TheStruct>("var"),
        //    };

        //    int value = 321;
        //    TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypePropertyReference, Value = value };

        //    TestSequence seq = new TestSequence()
        //    {
        //        Variables = { var },
        //        Activities =
        //        {
        //            testAssign,
        //        }
        //    };

        //    string error = string.Format(ErrorStrings.MemberNotFound, "PrivateProperty", typeof(TheStruct).Name);

        //    List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
        //    {
        //        new TestConstraintViolation(
        //            error,
        //            valueTypePropertyReference.ProductActivity)
        //    };

        //    VisualBasicSettings attachedSettings = new VisualBasicSettings();
        //    VisualBasic.SetSettings(seq.ProductActivity, attachedSettings);
        //    ExpressionUtil.AddImportReference(attachedSettings, typeof(TheStruct));

        //    TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        //}

        [Fact]
        public void TrySettingValueOfPropertyNameNull()
        {
            //  
            //  Test case description:
            //  Try executing ValueTypePropertyReference activity by setting PropertyName to null. Validation exception
            //  expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
            {
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypePropertyReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "PropertyName", valueTypePropertyReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypePropertyReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySettingPropertyOfNonExistentProperty()
        {
            //  
            //  Test case description:
            //  Try setting value of a property in a type which does not exist. Validation exception expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
            {
                PropertyName = "NonExistProperty",
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypePropertyReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.MemberNotFound, "NonExistProperty", typeof(TheStruct).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypePropertyReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySettingPropertyWithoutSetter()
        {
            //  
            //  Test case description:
            //  Try setting a property which does not have a setter. Exception expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
            {
                PropertyName = "PropertyWithoutSetter",
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypePropertyReference,
                Value = value,
                ExpectedOutcome = Outcome.UncaughtException(typeof(InvalidOperationException)),
            };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.MemberIsReadOnly, "PropertyWithoutSetter", typeof(TheStruct));

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypePropertyReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void ThrowExceptionFromSetterOfCustomTypeProperty()
        {
            //  
            //  Test case description:
            //  Try setting a property which throws from setter. 

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
            {
                PropertyName = "ThrowInSetterProperty",
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypePropertyReference,
                Value = value,
                ExpectedOutcome = Outcome.UncaughtException(typeof(System.Reflection.TargetInvocationException)),
            };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            TestRuntime.RunAndValidateAbortedException(seq, typeof(System.Reflection.TargetInvocationException), null);
        }

        [Fact]
        public void TrySettingNullOperand()
        {
            //  
            //  Test case description:
            //  Try setting a null OperandLocation. Validation exception expected.
            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
            {
                PropertyName = "PublicProperty",
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypePropertyReference,
                Value = value,
            };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "OperandLocation");

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypePropertyReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySettingNullPropertyName()
        {
            //  
            //  Test case description:
            //  Try setting a null PropertyName. Validation exception expected.
            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
            {
                OperandLocationVariable = var
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypePropertyReference,
                Value = value,
            };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "PropertyName", valueTypePropertyReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypePropertyReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySetReferenceTypeProperty()
        {
            //  
            //  Test case description:
            //  Try setting indexer on reference type. Validation exception is expected. 

            Variable<TheClass> var = new Variable<TheClass>("var", context => new TheClass());
            TestValueTypePropertyReference<TheClass, string> valueTypePropertyReference = new TestValueTypePropertyReference<TheClass, string>()
            {
                PropertyName = "StringProperty",
                OperandLocationVariable = var,
            };

            string value = "hello";
            TestAssign<string> testAssign = new TestAssign<string>()
            {
                ToLocation = valueTypePropertyReference,
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
                    valueTypePropertyReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void ChangePropertyNameAfterOpened()
        {
            Variable<TheStruct> var = new Variable<TheStruct>()
            {
                Name = "var",
                Default = new TheStruct()
            };

            TestValueTypePropertyReference<TheStruct, int> valueTypePropertyReference = new TestValueTypePropertyReference<TheStruct, int>()
            {
                PropertyName = "PublicProperty",
                OperandLocationVariable = var
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypePropertyReference,
                Value = value,
            };

            TestSequence sequence = new TestSequence()
            {
                Variables =
                {
                    var
                },
                Activities =
                {
                    new TestWriteLine("Start", "Start"),
                    new TestBlockingActivity("Blocking"),
                    testAssign,
                },
            };

            using (TestWorkflowRuntime runtime = TestRuntime.CreateTestWorkflowRuntime(sequence))
            {
                runtime.ExecuteWorkflow();
                runtime.WaitForActivityStatusChange("Blocking", TestActivityInstanceState.Executing);

                valueTypePropertyReference.PropertyName = "PublicProperty1";

                runtime.ResumeBookMark("Blocking", null);

                runtime.WaitForCompletion(true);
            }
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            Dictionary<string, object> results =
                WorkflowInvoker.Invoke((Activity)new ValueTypePropertyReference<TheStruct, int>() { PropertyName = "StaticProperty" }) as Dictionary<string, object>;

            Assert.NotNull(results["Result"]);
            Assert.NotNull(results["OperandLocation"]);
        }
    }
}
