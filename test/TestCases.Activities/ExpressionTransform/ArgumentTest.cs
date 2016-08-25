// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using System.Linq.Expressions;
using Test.Common.TestObjects.Activities.ExpressionTransform;
using Test.Common.TestObjects.Utilities;
using Xunit;

namespace TestCases.Activities.ExpressionTransform
{
    public class ArgumentTest
    {
        [Fact]
        public void GetValueInArgumentRValue()
        {
            Expression<Func<ActivityContext, int>> expression = (env) => env.GetValue(this.InArgument);
            Expression<Func<ActivityContext, int>> expression1 = (env) => env.GetValue<int>(this.InArgument);
            ArgumentValue<int> expectedActivity = new ArgumentValue<int>("InArgument");

            ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void GetValueOutArgumentRValue()
        {
            Expression<Func<ActivityContext, string>> expression = (env) => env.GetValue(this.OutArgument);
            Expression<Func<ActivityContext, string>> expression1 = (env) => env.GetValue<string>(this.OutArgument);
            ArgumentValue<string> expectedActivity = new ArgumentValue<string>("OutArgument");

            ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void GetValueInOutArgumentRValue()
        {
            Expression<Func<ActivityContext, string>> expression = (env) => env.GetValue(this.InOutArgument);
            Expression<Func<ActivityContext, string>> expression1 = (env) => env.GetValue<string>(this.InOutArgument);
            ArgumentValue<string> expectedActivity = new ArgumentValue<string>("InOutArgument");

            ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void GetValueBaseInArgumentRValue()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => env.GetValue(this.BaseInArgument);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("BaseInArgument");

            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void GetValueBaseOutArgumentRValue()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => env.GetValue(this.BaseOutArgument);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("BaseOutArgument");

            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void GetValueBaseInOutArgumentRValue()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => env.GetValue(this.BaseInOutArgument);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("BaseInOutArgument");

            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void GetValueBaseArgumentRValue()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => env.GetValue(this.BaseArgument);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("BaseArgument");

            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void GetValueInArgumentLValue()
        {
            NotSupportedException expectedException = new NotSupportedException(String.Format(ErrorStrings.UnsupportedReferenceExpressionType, ExpressionType.Call));
            Expression<Func<ActivityContext, int>> expression = (env) => env.GetValue(this.InArgument);

            ConvertReferenceAndValidate(expression, null, expectedException);
        }

        [Fact]
        public void GetValueCastBaseInArgumentRValue()
        {
            ValidationException expectedException = new ValidationException(ErrorStrings.ArgumentMustbePropertyofWorkflowElement);

            Expression<Func<ActivityContext, int>> expression = (env) => (int)env.GetValue((InArgument<int>)this.BaseInArgument);
            ConvertAndValidate(expression, null, expectedException);
        }

        [Fact]
        public void GetValueArgumentField()
        {
            ValidationException expectedException = new ValidationException(ErrorStrings.ArgumentMustbePropertyofWorkflowElement);

            Expression<Func<ActivityContext, string>> expression = (env) => env.GetValue(this.InOutArgumentField);
            ConvertAndValidate(expression, null, expectedException);
        }

        [Fact]
        public void InArgumentRValueGet()
        {
            Expression<Func<ActivityContext, int>> expression = (env) => this.InArgument.Get(env);
            Expression<Func<ActivityContext, int>> expression1 = (env) => this.InArgument.Get<int>(env);
            ArgumentValue<int> expectedActivity = new ArgumentValue<int>("InArgument");

            ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void OutArgumentRValueGet()
        {
            Expression<Func<ActivityContext, string>> expression = (env) => this.OutArgument.Get(env);
            Expression<Func<ActivityContext, string>> expression1 = (env) => this.OutArgument.Get<string>(env);
            ArgumentValue<string> expectedActivity = new ArgumentValue<string>("OutArgument");

            ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void InOutArgumentRValueGet()
        {
            Expression<Func<ActivityContext, string>> expression = (env) => this.InOutArgument.Get(env);
            Expression<Func<ActivityContext, string>> expression1 = (env) => this.InOutArgument.Get<string>(env);
            ArgumentValue<string> expectedActivity = new ArgumentValue<string>("InOutArgument");

            ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void BaseInArgumentRValueGet()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => this.BaseInArgument.Get(env);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("BaseInArgument");
            ConvertAndValidate(expression, expectedActivity, null);

            ArgumentValue<string> expectedActivity1 = new ArgumentValue<string>("BaseInArgument");
            Expression<Func<ActivityContext, string>> expression1 = (env) => this.BaseInArgument.Get<string>(env);
            ConvertAndValidate(expression1, expectedActivity1, null);
        }

        [Fact]
        public void BaseOutArgumentRValueGet()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => this.BaseOutArgument.Get(env);
            Expression<Func<ActivityContext, object>> expression1 = (env) => this.BaseOutArgument.Get<object>(env);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("BaseOutArgument");

            ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void BaseInOutArgumentRValueGet()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => this.BaseInOutArgument.Get(env);
            Expression<Func<ActivityContext, object>> expression1 = (env) => this.BaseInOutArgument.Get<object>(env);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("BaseInOutArgument");

            ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void BaseArgumentRValueGet()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => this.BaseArgument.Get(env);
            Expression<Func<ActivityContext, object>> expression1 = (env) => this.BaseArgument.Get<object>(env);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("BaseArgument");

            ConvertAndValidate(expression, expectedActivity, null); ConvertAndValidate(expression, expectedActivity, null);
            ConvertAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void GetValueArgumentLValue()
        {
            NotSupportedException expectedException = new NotSupportedException(
                String.Format(ErrorStrings.UnsupportedReferenceExpressionType, ExpressionType.Convert));

            Expression<Func<ActivityContext, int>> expression = (env) => (int)env.GetValue(this.BaseArgument);
            ConvertReferenceAndValidate(expression, null, expectedException);
        }

        [Fact]
        public void InArgumentLValueGet()
        {
            Expression<Func<ActivityContext, int>> expression = (env) => this.InArgument.Get(env);
            Expression<Func<ActivityContext, int>> expression1 = (env) => this.InArgument.Get<int>(env);
            ArgumentReference<int> expectedActivity = new ArgumentReference<int>("InArgument");

            ConvertReferenceAndValidate(expression, expectedActivity, null);
            ConvertReferenceAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void OutArgumentLValueGet()
        {
            Expression<Func<ActivityContext, string>> expression = (env) => this.OutArgument.Get(env);
            Expression<Func<ActivityContext, string>> expression1 = (env) => this.OutArgument.Get<string>(env);
            ArgumentReference<string> expectedActivity = new ArgumentReference<string>("OutArgument");

            ConvertReferenceAndValidate(expression, expectedActivity, null);
            ConvertReferenceAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void InOutArgumentLValueGet()
        {
            Expression<Func<ActivityContext, string>> expression = (env) => this.InOutArgument.Get(env);
            Expression<Func<ActivityContext, string>> expression1 = (env) => this.InOutArgument.Get<string>(env);
            ArgumentReference<string> expectedActivity = new ArgumentReference<string>("InOutArgument");

            ConvertReferenceAndValidate(expression, expectedActivity, null);
            ConvertReferenceAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void BaseInArgumentLValueGet()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => this.BaseInArgument.Get(env);
            Expression<Func<ActivityContext, object>> expression1 = (env) => this.BaseInArgument.Get<object>(env);
            ArgumentReference<object> expectedActivity = new ArgumentReference<object>("BaseInArgument");

            ConvertReferenceAndValidate(expression, expectedActivity, null);
            ConvertReferenceAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void BaseOutArgumentLValueGet()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => this.BaseOutArgument.Get(env);
            Expression<Func<ActivityContext, object>> expression1 = (env) => this.BaseOutArgument.Get<object>(env);
            ArgumentReference<object> expectedActivity = new ArgumentReference<object>("BaseOutArgument");

            ConvertReferenceAndValidate(expression, expectedActivity, null);
            ConvertReferenceAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void BaseInOutArgumentLValueGet()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => this.BaseInOutArgument.Get(env);
            Expression<Func<ActivityContext, object>> expression1 = (env) => this.BaseInOutArgument.Get<object>(env);
            ArgumentReference<object> expectedActivity = new ArgumentReference<object>("BaseInOutArgument");

            ConvertReferenceAndValidate(expression, expectedActivity, null);
            ConvertReferenceAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void BaseArgumentLValueGet()
        {
            Expression<Func<ActivityContext, object>> expression = (env) => this.BaseArgument.Get(env);
            Expression<Func<ActivityContext, object>> expression1 = (env) => this.BaseArgument.Get<object>(env);
            ArgumentReference<object> expectedActivity = new ArgumentReference<object>("BaseArgument");

            ConvertReferenceAndValidate(expression, expectedActivity, null);
            ConvertReferenceAndValidate(expression1, expectedActivity, null);
        }

        [Fact]
        public void ArgumentFieldGet()
        {
            ValidationException expectedException = new ValidationException(ErrorStrings.ArgumentMustbePropertyofWorkflowElement);

            Expression<Func<ActivityContext, string>> expression = (env) => this.InOutArgumentField.Get(env);
            ConvertAndValidate(expression, null, expectedException);
        }

        [Fact]
        public void GetValueRuntimeArgumentRValue()
        {
            Variable<int> var = new Variable<int>();
            RuntimeArgument ra = new RuntimeArgument("InArgument", typeof(int), ArgumentDirection.In);
            Expression<Func<ActivityContext, object>> expression = (env) => env.GetValue(ra);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("InArgument");

            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void RuntimeArgumentRValueGet()
        {
            RuntimeArgument ra = new RuntimeArgument("InArgument", typeof(int), ArgumentDirection.In);
            Expression<Func<ActivityContext, object>> expression = (env) => env.GetValue(ra);
            ArgumentValue<object> expectedActivity = new ArgumentValue<object>("InArgument");

            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void DelegateInArgumentRValueGet()
        {
            DelegateInArgument<string> delArg = new DelegateInArgument<string>();

            Expression<Func<ActivityContext, string>> expression = (env) => delArg.Get(env);
            DelegateArgumentValue<string> expectedActivity = new DelegateArgumentValue<string>(delArg);

            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void BaseDelegateInArgumentRValueGet()
        {
            DelegateInArgument delArg = new DelegateInArgument<string>();

            Expression<Func<ActivityContext, object>> expression = (env) => delArg.Get(env);
            DelegateArgumentValue<object> expectedActivity = new DelegateArgumentValue<object>(delArg);

            ConvertAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void DelegateOutArgumentLValueGet()
        {
            DelegateOutArgument<string> delArg = new DelegateOutArgument<string>();

            Expression<Func<ActivityContext, string>> expression = (env) => delArg.Get(env);
            DelegateArgumentReference<string> expectedActivity = new DelegateArgumentReference<string>(delArg);

            ConvertReferenceAndValidate(expression, expectedActivity, null);
        }

        [Fact]
        public void BaseDelegateOutArgumentLValueGet()
        {
            DelegateOutArgument delArg = new DelegateOutArgument<string>();

            Expression<Func<ActivityContext, object>> expression = (env) => delArg.Get(env);
            DelegateArgumentReference<object> expectedActivity = new DelegateArgumentReference<object>(delArg);

            ConvertReferenceAndValidate(expression, expectedActivity, null);
        }

        public InOutArgument<string> InOutArgumentField = new InOutArgument<string>();

        public Argument BaseArgument { get; set; }
        public InArgument BaseInArgument { get; set; }
        public OutArgument BaseOutArgument { get; set; }
        public InOutArgument BaseInOutArgument { get; set; }

        public InArgument<int> InArgument { get; set; }
        public OutArgument<string> OutArgument { get; set; }
        public InOutArgument<string> InOutArgument { get; set; }

        public static Activity ConvertAndValidate<TResult>(Expression<Func<ActivityContext, TResult>> expr, Activity expectedActivity, Exception expectedException)
        {
            bool expectSuccess = expectedException == null ? true : false;
            Activity act = ExpressionTestRuntime.Convert(expr, expectedException);
            if (expectSuccess)
            {
                ExpressionTestRuntime.ValidateActivity(expectedActivity, act);
            }

            act = ExpressionTestRuntime.TryConvert(expr, expectSuccess);
            if (expectSuccess)
            {
                ExpressionTestRuntime.ValidateActivity(expectedActivity, act);
            }

            return act;
        }

        public static Activity ConvertReferenceAndValidate<TResult>(Expression<Func<ActivityContext, TResult>> expr, Activity expectedActivity, Exception expectedException)
        {
            bool expectSuccess = expectedException == null ? true : false;
            Activity act = ExpressionTestRuntime.ConvertReference(expr, expectedException);
            if (expectSuccess)
            {
                ExpressionTestRuntime.ValidateActivity(act, expectedActivity);
            }

            act = ExpressionTestRuntime.TryConvertReference(expr, expectSuccess);
            if (expectSuccess)
            {
                ExpressionTestRuntime.ValidateActivity(act, expectedActivity);
            }

            return act;
        }
    }
}
