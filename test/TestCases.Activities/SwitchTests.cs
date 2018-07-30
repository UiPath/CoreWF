// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using System.Collections.Generic;
using Test.Common.TestObjects.Activities;
using Test.Common.TestObjects.Activities.Tracing;
using Test.Common.TestObjects.Activities.Variables;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Runtime.ConstraintValidation;
using Test.Common.TestObjects.Utilities;
using TestCases.Activities.Common;
using Xunit;

namespace TestCases.Activities
{
    public enum OrderStatus
    {
        NewOrder = 1,
        Processing = 2,
        Shipped = 3
    };

    public class SwitchTests
    {
        [Fact]
        public void SwitchNoDefault()
        {
            //  SwitchNoDefaultSimple switch case scenario
            //  Test case description:
            //  Simple switch case scenario

            TestSwitch<int> switchAct = new TestSwitch<int>();
            Variable<int> switchExpression = new Variable<int>("switchExpression", 123);
            TestSequence seq = new TestSequence();
            seq.Variables.Add(switchExpression);

            switchAct.ExpressionVariable = switchExpression;
            switchAct.AddCase(12, new TestProductWriteline { Text = "in case 12" });
            switchAct.AddCase(23, new TestProductWriteline { Text = "in case 23" });
            switchAct.AddCase(123, new TestProductWriteline { Text = "in case 123" });
            switchAct.AddCase(234, new TestProductWriteline { Text = "in case 234" });

            seq.Activities.Add(switchAct);
            switchAct.Hints.Add(2);
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// Empty switch activity run it by itself, run it within other scopes
        /// </summary>        
        [Fact]
        public void EmptySwitch()
        {
            TestSwitch<string> switchAct = new TestSwitch<string>();
            switchAct.Hints.Add(-1);
            switchAct.Expression = "";
            TestRuntime.RunAndValidateWorkflow(switchAct);
        }

        [Fact]
        public void SwitchDefault()
        {
            //  SwitchDefaultSimple switch case default 
            //  Test case description:
            //  Simple switch case default 
            TestSwitch<int> switchAct = new TestSwitch<int>();
            Variable<int> switchExpression = new Variable<int>("switchExpression", 444);
            TestSequence seq = new TestSequence();
            seq.Variables.Add(switchExpression);

            switchAct.ExpressionVariable = switchExpression;
            switchAct.AddCase(12, new TestProductWriteline { Text = "in case 12" });
            switchAct.AddCase(23, new TestProductWriteline { Text = "in case 23" });
            switchAct.AddCase(123, new TestProductWriteline { Text = "in case 123" });
            switchAct.AddCase(234, new TestProductWriteline { Text = "in case 234" });
            switchAct.Default = new TestProductWriteline { Text = "in default" };

            switchAct.Hints.Add(-1);
            seq.Activities.Add(switchAct);
            TestRuntime.RunAndValidateWorkflow(seq);
        }

        [Fact]
        public void SwitchStandAlone()
        {
            //  SwitchNoDefaultSimple switch case scenario
            //  Test case description:
            //  Simple switch case scenario

            TestSwitch<int> switchAct = new TestSwitch<int>
            {
                DisplayName = "standAloneSwitch",
                Expression = 23
            };
            switchAct.AddCase(12, new TestProductWriteline { Text = "in case 12" });
            switchAct.AddCase(23, new TestProductWriteline { Text = "in case 23" });
            switchAct.AddCase(123, new TestProductWriteline { Text = "in case 123" });
            switchAct.AddCase(234, new TestProductWriteline { Text = "in case 234" });

            switchAct.Hints.Add(1);
            TestRuntime.RunAndValidateWorkflow(switchAct);
        }

        [Fact]
        public void SwitchNoCaseButDefault()
        {
            //  SwitchNoCaseButDefaultSimple switch no cases but default
            //  Test case description:
            //  Simple switch no cases but default

            TestSwitch<int> switchAct = new TestSwitch<int>();
            Variable<int> switchExpression = new Variable<int>("switchExpression", 444);
            TestSequence seq = new TestSequence();
            seq.Variables.Add(switchExpression);

            switchAct.ExpressionVariable = switchExpression;
            switchAct.Default = new TestProductWriteline { Text = "in default" };

            switchAct.Hints.Add(-1);
            seq.Activities.Add(switchAct);

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// SwitchWithEnumsSwitch with enums and enums with flags
        /// Switch with enums and enums with flags
        /// </summary>        
        [Fact]
        public void SwitchWithEnums()
        {
            TestSwitch<OrderStatus> order = new TestSwitch<OrderStatus>();
            order.AddCase(OrderStatus.NewOrder, new TestWriteLine("We have received a new order") { Message = "New Order" });
            order.AddCase(OrderStatus.Processing, new TestWriteLine("Order is in processing state") { Message = "Processing" });
            order.AddCase(OrderStatus.Shipped, new TestSequence { Activities = { new TestWriteLine("Order is shipped to you") { Message = "Order shipped" } } });
            order.Hints.Add(0);
            order.Hints.Add(1);
            order.Hints.Add(2);

            List<OrderStatus> values = new List<OrderStatus>() { OrderStatus.NewOrder, OrderStatus.Processing, OrderStatus.Shipped };
            DelegateInArgument<OrderStatus> var = new DelegateInArgument<OrderStatus> { Name = "var" };

            TestForEach<OrderStatus> forEachAct = new TestForEach<OrderStatus>("ForEachAct")
            {
                Values = values,
                CurrentVariable = var
            };
            TestSequence seq = new TestSequence("Seq in For Each");
            seq.Activities.Add(order);
            forEachAct.Body = seq;
            order.ExpressionExpression = (env) => (OrderStatus)var.Get(env);

            forEachAct.HintIterationCount = 3;

            TestRuntime.RunAndValidateWorkflow(forEachAct);
        }

        /// <summary>
        /// Switch evaluating a case null
        /// </summary>        
        [Fact]
        public void SwitchEvaluatingNullCase()
        {
            TestSwitch<string> sw = new TestSwitch<string>();
            string s = null;

            sw.AddCase(null, new TestWriteLine() { Message = "Hi" });
            sw.Hints.Add(0);
            sw.Expression = s;

            TestRuntime.RunAndValidateWorkflow(sw);
        }

        ///// <summary>
        ///// SwitchWithUserDefinedTypesSwitch with user defined types
        ///// Switch with user defined types
        ///// </summary>        
        //[Fact]
        //public void SwitchWithUserDefinedTypes()
        //{
        //    TestSwitch<UserClass> switchAct = new TestSwitch<UserClass>();
        //    UserClass userClass = new UserClass();
        //    userClass.Name = "Max";

        //    UserClass userClass1 = new UserClass();
        //    userClass1.Name = "Rob";

        //    UserClass userClass2 = new UserClass();
        //    userClass2.Name = "Rob and Max";

        //    switchAct.AddCase(userClass, new TestWriteLine() { Message = "Max" });
        //    switchAct.AddCase(userClass1, new TestWriteLine() { Message = "Rob" });
        //    switchAct.AddCase(userClass2, new TestWriteLine() { Message = "Rob And Max" });

        //    switchAct.ExpressionActivity = new TestVisualBasicValue<UserClass>(" New UserClass With {.Name = \"Rob and Max\"}");

        //    switchAct.Hints.Add(2);

        //    VisualBasicSettings attachedSettings = new VisualBasicSettings();
        //    VisualBasic.SetSettings(switchAct.ProductActivity, attachedSettings);
        //    ExpressionUtil.AddImportReference(attachedSettings, typeof(TheStruct));

        //    TestRuntime.RunAndValidateWorkflow(switchAct);
        //}

        /// <summary>
        /// ThrowExceptionInCaseThrow exception in the case that will be executed
        /// Throw exception in the case that will be executed
        /// </summary>        
        [Fact]
        public void ThrowExceptionInCase()
        {
            TestSwitch<float> switchAct = new TestSwitch<float>
            {
                DisplayName = "Switch Act"
            };
            switchAct.AddCase(123, new TestThrow<InvalidCastException>("THrow invalid cast") { ExpectedOutcome = Outcome.UncaughtException(typeof(InvalidCastException)) });
            switchAct.Expression = 123;
            switchAct.Hints.Add(0);

            TestRuntime.RunAndValidateAbortedException(switchAct, typeof(InvalidCastException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Throw exception in the case that wont be executed (another case will be hit)
        /// </summary>        
        [Fact]
        public void ThrowExcInCaseWhichWontExecute()
        {
            TestSwitch<float> switchAct = new TestSwitch<float>();
            switchAct.AddCase(345678, new TestWriteLine("Writeline") { Message = "345678" });
            switchAct.AddCase(123, new TestThrow<InvalidCastException>("Throw invalid cast"));
            switchAct.Expression = 345678;
            switchAct.Hints.Add(0);

            TestRuntime.RunAndValidateWorkflow(switchAct);
        }

        /// <summary>
        /// ThrowExceptionInExpressionThrow exception in expression
        /// Throw exception in expression
        /// </summary>        
        [Fact]
        public void ThrowExceptionInExpression()
        {
            TestSwitch<int> switchAct = new TestSwitch<int>();
            Variable<int> temp = VariableHelper.CreateInitialized<int>("temp", 3);
            TestSequence seq = new TestSequence();

            seq.Activities.Add(switchAct);
            seq.Variables.Add(temp);

            switchAct.AddCase(123, new TestSequence("Seq"));
            switchAct.ExpressionExpression = (env) => (int)(1 / (temp.Get(env) - 3));
            switchAct.Hints.Add(-1);
            switchAct.ExpectedOutcome = Outcome.UncaughtException(typeof(DivideByZeroException));

            TestRuntime.RunAndValidateAbortedException(seq, typeof(DivideByZeroException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Float evaluates 1/0 as infinity and does not throw exception
        /// </summary>
        [Fact]
        public void ThrowWithCaseInfinity()
        {
            TestSwitch<float> switchAct = new TestSwitch<float>();
            Variable<float> temp = VariableHelper.CreateInitialized<float>("temp", 3);
            float temp1 = 1;
            float temp2 = 0;
            TestSequence seq = new TestSequence();

            seq.Activities.Add(switchAct);
            seq.Variables.Add(temp);

            switchAct.AddCase(123, new TestWriteLine("Seq") { Message = "" });
            switchAct.AddCase(temp1 / temp2, new TestWriteLine() { Message = "Infinity is the value" });
            switchAct.ExpressionExpression = (env) => (float)(1 / (temp.Get(env) - 3));
            switchAct.Hints.Add(1);

            TestRuntime.RunAndValidateWorkflow(seq);
        }

        /// <summary>
        /// SwitchThrowInDefaultThrow exception in default case
        /// Throw exception in default case
        /// </summary>        
        [Fact]
        public void SwitchThrowInDefault()
        {
            TestSwitch<float> switchAct = new TestSwitch<float>();
            switchAct.AddCase(123, new TestThrow<InvalidCastException>("Throw invalid cast") { ExpectedOutcome = Outcome.None });
            switchAct.Default = new TestThrow<TestCaseException>("Op cancelled") { ExpectedOutcome = Outcome.UncaughtException(typeof(TestCaseException)) };
            switchAct.Expression = 456;
            switchAct.Hints.Add(-1);
            TestRuntime.RunAndValidateAbortedException(switchAct, typeof(TestCaseException), new Dictionary<string, string>());
        }

        /// <summary>
        /// Switch with WorkflowInvoker
        /// </summary>        
        [Fact]
        public void SwitchWithWorkflowInvoker()
        {
            TestSwitch<int> switchAct = new TestSwitch<int>();
            switchAct.AddCase(12, new TestProductWriteline { Text = "in case 12" });
            switchAct.AddCase(23, new TestProductWriteline { Text = "in case 23" });
            switchAct.AddCase(123, new TestProductWriteline { Text = "in case 123" });
            switchAct.AddCase(234, new TestProductWriteline { Text = "in case 234" });
            switchAct.Hints.Add(2);

            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("Expression", 123);
            TestRuntime.RunAndValidateUsingWorkflowInvoker(switchAct, dic, null, null);
        }

        /// <summary>
        /// Switch with empty case
        /// </summary>        
        [Fact]
        public void SwitchWithEmptyCase()
        {
            TestSwitch<string> switchAct = new TestSwitch<string>();
            switchAct.AddCase("12", new TestProductWriteline { Text = "in case 12" });
            switchAct.AddCase("", new TestProductWriteline { Text = "in case 12" });
            switchAct.Hints.Add(1);
            switchAct.Expression = "";
            TestRuntime.RunAndValidateWorkflow(switchAct);
        }

        /// <summary>
        /// Switch with no Expression set
        ///                     new Switch
        ///                     {
        ///                         Cases =
        ///                         {
        ///                             {"abc", new Sequence()}
        ///                         }
        ///                     }
        /// </summary>        
        [Fact]
        public void SwitchWithNoExpressionSet()
        {
            TestSwitch<string> switchAct = new TestSwitch<string>();
            switchAct.Hints.Add(-1);
            switchAct.AddCase("1", new TestProductWriteline { Text = "in case 1" });
            ((CoreWf.Statements.Switch<string>)switchAct.ProductActivity).Expression = null;

            string exceptionMessage = string.Format(ErrorStrings.RequiredArgumentValueNotSupplied, "Expression");
            TestRuntime.ValidateWorkflowErrors(switchAct, new List<TestConstraintViolation>(), typeof(ArgumentException), exceptionMessage);
        }

        [Fact]
        public void DifferentArguments()
        {
            //Testing Different argument types for Switch.Expression
            // DelegateInArgument
            // DelegateOutArgument
            // Activity<T>
            // Variable<T> , Activity<T> and Expression is already implemented.

            DelegateInArgument<string> delegateInArgument = new DelegateInArgument<string>("Input");
            DelegateOutArgument<string> delegateOutArgument = new DelegateOutArgument<string>("Output");

            TestCustomActivity<InvokeFunc<string, string>> invokeFunc = TestCustomActivity<InvokeFunc<string, string>>.CreateFromProduct(
               new InvokeFunc<string, string>
               {
                   Argument = "PassedInValue",
                   Func = new ActivityFunc<string, string>
                   {
                       Argument = delegateInArgument,
                       Result = delegateOutArgument,
                       Handler = new CoreWf.Statements.Sequence
                       {
                           DisplayName = "Sequence1",
                           Activities =
                             {
                                 new CoreWf.Statements.Switch<string>
                                 {
                                     DisplayName = "Switch1",
                                     Expression = delegateInArgument,
                                     Cases =
                                     {
                                         {
                                             "PassedInValue",
                                             new CoreWf.Statements.Assign<string>
                                             {
                                                 DisplayName = "Assign1",
                                                 To = delegateOutArgument,
                                                 Value = "OutValue",
                                             }
                                          },
                                     } ,
                                    Default = new Test.Common.TestObjects.CustomActivities.WriteLine{ DisplayName = "W1", Message = "This should not be printed" },
                                 },
                                 new CoreWf.Statements.Switch<string>
                                 {
                                     DisplayName = "Switch2",
                                     Expression = delegateOutArgument,
                                     Cases =
                                     {
                                         {
                                             "OutValue",
                                             new Test.Common.TestObjects.CustomActivities.WriteLine{ DisplayName = "W2" ,Message = delegateOutArgument }
                                         }
                                     },
                                     Default = new Test.Common.TestObjects.CustomActivities.WriteLine{ DisplayName = "W3", Message = "This should not be printed"},
                                 }
                             }
                       }
                   }
               }
               );

            TestSwitch<string> switch1 = new TestSwitch<string>
            {
                DisplayName = "Switch1",
                Hints = { 0 }
            };
            switch1.AddCase("PassedInValue", new TestAssign<string> { DisplayName = "Assign1" });
            switch1.Default = new TestWriteLine { DisplayName = "W1" };

            TestSwitch<string> switch2 = new TestSwitch<string>
            {
                DisplayName = "Switch2",
                Hints = { 0 }
            };
            switch2.AddCase("OutValue", new TestWriteLine { DisplayName = "W2", HintMessage = "OutValue" });
            switch2.Default = new TestWriteLine { DisplayName = "W3" };

            TestSequence sequenceForTracing = new TestSequence
            {
                DisplayName = "Sequence1",
                Activities =
                {
                    switch1,
                    switch2,
                }
            };
            invokeFunc.CustomActivityTraces.Add(sequenceForTracing.GetExpectedTrace().Trace);

            TestRuntime.RunAndValidateWorkflow(invokeFunc);
        }
    }
}
