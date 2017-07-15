// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf;
using CoreWf.Expressions;
using System.Collections.Generic;
using System.IO;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Expressions;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;
using TestCases.Activities.Common.Expressions;
using Xunit;

namespace TestCases.Activities.Expressions
{
    public class ValueTypeFieldReference
    {
        [Fact]
        public void SetPublicFieldOnValueType()
        {
            //  
            //  Test case description:
            //  Set a public field on value type.
            TheStruct valueType = new TheStruct();
            valueType.publicField = 123;

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                FieldName = "publicField",
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx).publicField.ToString()), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void SetPublicEnumFieldOnValueType()
        {
            //  
            //  Test case description:
            //  Set a public enum field on value type.

            TheStruct valueType = new TheStruct();
            valueType.enumField = FileAccess.Write;

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeFieldReference<TheStruct, FileAccess> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, FileAccess>()
            {
                FieldName = "enumField",
                OperandLocationVariable = var,
            };

            System.IO.FileAccess value = System.IO.FileAccess.ReadWrite;
            TestAssign<System.IO.FileAccess> testAssign = new TestAssign<FileAccess>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx).enumField.ToString()), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void SetStaticFieldOnValueType()
        {
            //  
            //  Test case description:
            //  Set a public static field on value type.

            TheStruct.staticField = 123;

            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                FieldName = "staticField",
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => TheStruct.staticField.ToString() ), HintMessage = value.ToString() }
                }
            };

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        //[Fact]
        //public void TrySettingPropertyNotField()
        //{
        //    //  
        //    //  Test case description:
        //    //  Try setting a property of a value type by this activity. Validation exception.

        //    TheStruct valueType = new TheStruct();

        //    Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
        //    TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
        //    {
        //        FieldName = "PublicProperty",
        //        OperandLocation = new TestVisualBasicReference<TheStruct>("var"),
        //    };

        //    int value = 321;
        //    TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

        //    TestSequence seq = new TestSequence()
        //    {
        //        Variables = { var },
        //        Activities =
        //        {
        //            testAssign,
        //        }
        //    };

        //    string error = string.Format(ErrorStrings.MemberNotFound, "PublicProperty", typeof(TheStruct).Name);

        //    List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
        //    {
        //        new TestConstraintViolation(
        //            error,
        //            valueTypeFieldReference.ProductActivity)
        //    };

        //    VisualBasicSettings attachedSettings = new VisualBasicSettings();
        //    VisualBasic.SetSettings(seq.ProductActivity, attachedSettings);
        //    ExpressionUtil.AddImportReference(attachedSettings, typeof(TheStruct));
        //    TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        //}

        [Fact]
        public void TrySettingPrivateFieldOnValueType()
        {
            //  
            //  Test case description:
            //  Try setting a private field on value type. Validation exception expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                FieldName = "privateField",
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.MemberNotFound, "privateField", typeof(TheStruct).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeFieldReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySettingValueOfFieldNameNull()
        {
            //  
            //  Test case description:
            //  Try executing ValueTypeFieldReference activity by setting FieldName to null. Validation exception
            //  expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                FieldName = null,
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "FieldName", valueTypeFieldReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeFieldReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySettingFieldOfNonExistentField()
        {
            //  
            //  Test case description:
            //  Try setting value of a field in a type which does not exist. Validation exception expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                FieldName = "NonExistField",
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                }
            };

            string error = string.Format(ErrorStrings.MemberNotFound, "NonExistField", typeof(TheStruct).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeFieldReference.ProductActivity)
            };


            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySettingReadOnlyField()
        {
            //  
            //  Test case description:
            //  Try setting a read only field. Exception expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                FieldName = "readonlyField",
                OperandLocationVariable = var,
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx).readonlyField.ToString()), HintMessage = value.ToString() }
                }
            };

            string error = string.Format(ErrorStrings.MemberIsReadOnly, "readonlyField", typeof(TheStruct).Name);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeFieldReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySettingNullOperand()
        {
            //  
            //  Test case description:
            //  Try setting a null OperandLocation. Validation exception expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                FieldName = "publicField",
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx).publicField.ToString()), HintMessage = value.ToString() }
                }
            };

            string error = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "OperandLocation");

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeFieldReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySettingNullFieldName()
        {
            //  
            //  Test case description:
            //  Try setting a null OperandLocation. Validation exception expected.

            TheStruct valueType = new TheStruct();

            Variable<TheStruct> var = new Variable<TheStruct>() { Default = valueType, Name = "var" };
            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                OperandLocationVariable = var
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>() { ToLocation = valueTypeFieldReference, Value = value };

            TestSequence seq = new TestSequence()
            {
                Variables = { var },
                Activities =
                {
                    testAssign,
                    new TestWriteLine { MessageExpression = ((ctx) => var.Get(ctx).publicField.ToString()), HintMessage = value.ToString() }
                }
            };

            string error = string.Format(ErrorStrings.ActivityPropertyMustBeSet, "FieldName", valueTypeFieldReference.DisplayName);

            List<TestConstraintViolation> constraints = new List<TestConstraintViolation>
            {
                new TestConstraintViolation(
                    error,
                    valueTypeFieldReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void TrySetReferenceTypeField()
        {
            //  
            //  Test case description:
            //  Try setting indexer on reference type. Validation exception is expected. 
            Variable<TheClass> var = new Variable<TheClass>("var", context => new TheClass());
            TestValueTypeFieldReference<TheClass, string> valueTypeFieldReference = new TestValueTypeFieldReference<TheClass, string>()
            {
                FieldName = "stringField",
                OperandLocationVariable = var,
            };

            string value = "hello";
            TestAssign<string> testAssign = new TestAssign<string>()
            {
                ToLocation = valueTypeFieldReference,
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
                    valueTypeFieldReference.ProductActivity)
            };

            TestRuntime.ValidateWorkflowErrors(seq, constraints, error);
        }

        [Fact]
        public void ChangeFieldNameAfterOpened()
        {
            Variable<TheStruct> var = new Variable<TheStruct>()
            {
                Name = "var",
                Default = new TheStruct()
            };

            TestValueTypeFieldReference<TheStruct, int> valueTypeFieldReference = new TestValueTypeFieldReference<TheStruct, int>()
            {
                FieldName = "publicField",
                OperandLocationVariable = var
            };

            int value = 321;
            TestAssign<int> testAssign = new TestAssign<int>()
            {
                ToLocation = valueTypeFieldReference,
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

                valueTypeFieldReference.FieldName = "intField1";

                runtime.ResumeBookMark("Blocking", null);

                runtime.WaitForCompletion(true);
            }
        }

        [Fact]
        public void InvokeWithWorkflowInvoker()
        {
            Dictionary<string, object> results =
                WorkflowInvoker.Invoke((Activity)new ValueTypeFieldReference<TheStruct, int>() { FieldName = "staticField" }) as Dictionary<string, object>;

            Assert.NotNull(results["Result"]);
            Assert.NotNull(results["OperandLocation"]);
        }
    }
}
