// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Statements;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using Test.Common.TestObjects.Runtime;
using Test.Common.TestObjects.Utilities;

namespace Test.Common.TestObjects.Activities.ExpressionTransform
{
    public class ExpressionTestRuntime
    {
        private static readonly string s_path = string.Empty; //DirectoryAssistance.GetTestBinsDirectory("TempFile.txt");

        public static void ValidateExpressionXaml<T>(TestExpression te)
        {
            Activity expectedActivity = null, actualActivity = null;

            Expression<Func<ActivityContext, T>> lambdaExpression = null;

            lambdaExpression = (Expression<Func<ActivityContext, T>>)te.CreateLambdaExpresson<T>();
            //Log.TraceInternal("Expression: {0}", lambdaExpression.ToString());

            expectedActivity = te.CreateExpectedActivity() as Activity;

            if (te.ExpectedConversionException != null)
            {
                ExceptionHelpers.CheckForException(
                    te.ExpectedConversionException.GetType(), te.ExpectedConversionException.Message,
                    () => { actualActivity = ExpressionServices.Convert(lambdaExpression); }, true);
            }
            else
            {
                actualActivity = ExpressionServices.Convert(lambdaExpression);
                ValidateActivity(expectedActivity, actualActivity);
            }
        }

        public static void ValidateReferenceExpressionXaml<T>(TestExpression te)
        {
            Expression<Func<ActivityContext, T>> lambdaExpression = (Expression<Func<ActivityContext, T>>)te.CreateLambdaExpresson<T>();
            //Log.TraceInternal("Expression: {0}", lambdaExpression.ToString());

            Activity expectedActivity = te.CreateExpectedActivity() as Activity;
            Activity actualActivity = ExpressionServices.ConvertReference(lambdaExpression);

            ValidateActivity(expectedActivity, actualActivity);
        }

        public static void ValidateActivity(Activity expected, Activity actual)
        {
            //string expectedXaml = PartialTrustXamlServices.Save(expected);
            ////Log.TraceInternal("Expected Xaml: \n{0}", expectedXaml);

            //string actualXaml = PartialTrustXamlServices.Save(actual);
            ////Log.TraceInternal("Actual Xaml: \n{0}", actualXaml);

            //if (actualXaml != expectedXaml)
            //    throw new Exception("Expected Xaml and actual Xaml do not match!");
        }

        public static void ModifyValidateAndExecuteActivity(Sequence expectedSequence, Sequence actualSequence, string expectedResult)
        {
            // clone the WF before removing the WriteLine in it.
            //string tempXaml = PartialTrustXamlServices.Save(actualSequence);

            //// remove the WriteLine in actual sequence.
            //RemoveAllWriteLinesFromSequence(actualSequence);

            //string expectedXaml = PartialTrustXamlServices.Save(expectedSequence);
            ////Log.TraceInternal("Expected Xaml: \n{0}", expectedXaml);

            //string actualXaml = PartialTrustXamlServices.Save(actualSequence);
            ////Log.TraceInternal("Actual Xaml: \n{0}", actualXaml);

            //if (actualXaml != expectedXaml)
            //    throw new Exception("Expected Xaml and actual Xaml do not match!");

            //Exception actualException = null;
            //Activity clonedWorkflowElement = (Activity)PartialTrustXamlServices.Load(new StringReader(tempXaml));
            //string actualResult = ExecuteWorkflowAndGetResult(clonedWorkflowElement, out actualException);
            ////Log.TraceInternal("Actual result: {0}\n", actualResult);

            //if (actualException != null)
            //{
            //    throw new Exception("Exception when running actual workflow", actualException);
            //}

            //if (expectedResult != actualResult)
            //{
            //    //Log.TraceInternal("Expected result: {0}\n", expectedResult);
            //    throw new Exception("Inconsistent execution result!");
            //}
        }

        // variablesActual is set when variable comparison makes the difference. Example: memberExpression.NoneGenericVariableGet
        public static void ValidateExecutionResult(TestExpression te, List<Variable> variablesExpected, List<Variable> variablesActual, params Type[] refTypes)
        {
            //MethodInfo createExpectedWorkflowMethodInfo = te.GetType().GetMethod("CreateExpectedWorkflow", BindingFlags.Public | BindingFlags.Instance);
            //Sequence expectedSequence = (Sequence)createExpectedWorkflowMethodInfo.MakeGenericMethod(te.ResultType).Invoke(te, BindingFlags.Instance | BindingFlags.InvokeMethod, null, null, null);
            //if (variablesExpected != null)
            //{
            //    foreach (Variable v in variablesExpected)
            //    {
            //        expectedSequence.Variables.Add(v);
            //    }
            //}

            //string xaml = PartialTrustXamlServices.Save(expectedSequence);
            ////Log.TraceInternal("Expected workflow:\n{0}", xaml);

            //Activity clonedWorkflowElement = (Activity)PartialTrustXamlServices.Load(new StringReader(xaml));

            //var namespaces = from t in refTypes
            //                 select t.Namespace;

            //var assemblies = from t in refTypes
            //                 select new AssemblyReference() { Assembly = t.Assembly };

            //// TextExpression.SetNamespaces(clonedWorkflowElement, namespaces.ToArray());
            //// TextExpression.SetReferences(clonedWorkflowElement, assemblies.ToArray());

            //Exception expectedException = null;
            //string expectedResult = ExecuteWorkflowAndGetResult(clonedWorkflowElement, out expectedException);
            ////Log.TraceInternal("Expected result: {0}\n", expectedResult);

            //MethodInfo createActualWorkflowMethodInfo = te.GetType().GetMethod("CreateActualWorkflow", BindingFlags.Public | BindingFlags.Instance);
            //Sequence actualSequence = (Sequence)createActualWorkflowMethodInfo.MakeGenericMethod(te.ResultType).Invoke(te, BindingFlags.Instance | BindingFlags.InvokeMethod, null, null, null);
            //// TextExpression.SetNamespaces(actualSequence, namespaces.ToArray());
            //// TextExpression.SetReferences(actualSequence, assemblies.ToArray());

            //List<Variable> varList = variablesActual ?? variablesExpected;
            //if (varList != null)
            //{
            //    foreach (Variable v in varList)
            //    {
            //        actualSequence.Variables.Add(v);
            //    }
            //}

            //// xaml = PartialTrustXamlServices.Save(actualSequence);
            ////Log.TraceInternal("Actual workflow:\n{0}", xaml);
            //// clonedWorkflowElement = (Sequence)XamlServices.Load(new StringReader(xaml));
            //Exception actualException = null;
            //string actualResult = ExecuteWorkflowAndGetResult(actualSequence, out actualException);
            ////Log.TraceInternal("Actual result: {0}\n", actualResult);

            //ExpressionTestRuntime.ValidateException(expectedException, actualException);

            //if (expectedResult != actualResult)
            //{
            //    throw new Exception("Inconsistent execution result!");
            //}
        }

        public static void ValidateExecutionResult(TestExpression te, List<Variable> variables, params Type[] refTypes)
        {
            ValidateExecutionResult(te, variables, null, refTypes);
        }

        public static Activity<TResult> Convert<TResult>(Expression<Func<ActivityContext, TResult>> expression, Exception expectedException)
        {
            Activity<TResult> result = null;

            if (expectedException != null)
            {
                ExceptionHelpers.CheckForException(
                    expectedException.GetType(), expectedException.Message,
                    () => { result = ExpressionServices.Convert<TResult>(expression); }, true);
            }
            else
            {
                result = ExpressionServices.Convert<TResult>(expression);
            }

            return result;
        }

        public static Activity<TResult> TryConvert<TResult>(Expression<Func<ActivityContext, TResult>> expression, bool expectSucceeded)
        {
            bool isSucceeded;

            isSucceeded = ExpressionServices.TryConvert<TResult>(expression, out Activity<TResult> resultWorkflow);
            if (isSucceeded != expectSucceeded)
                throw new Exception(string.Format("Expected return is {0}, but actual return is {1}", expectSucceeded, isSucceeded));

            if (false == isSucceeded && resultWorkflow != null)
            {
                throw new Exception("TryConvert fails, but has a non-null workflow result");
            }
            else if (true == isSucceeded && resultWorkflow == null)
            {
                throw new Exception("TryConvert succeeded, but has a null workflow result");
            }

            return resultWorkflow;
        }

        public static Activity<Location<TResult>> ConvertReference<TResult>(Expression<Func<ActivityContext, TResult>> expression, Exception expectedException)
        {
            Activity<Location<TResult>> result = null;

            if (expectedException != null)
            {
                ExceptionHelpers.CheckForException(
                    expectedException.GetType(), expectedException.Message,
                    () => { result = ExpressionServices.ConvertReference<TResult>(expression); }, true);
            }
            else
            {
                result = ExpressionServices.ConvertReference<TResult>(expression);
            }

            return result;
        }

        public static Activity<Location<TResult>> TryConvertReference<TResult>(Expression<Func<ActivityContext, TResult>> expression, bool expectSucceeded)
        {
            bool isSucceeded;

            isSucceeded = ExpressionServices.TryConvertReference<TResult>(expression, out Activity<Location<TResult>> resultWorkflow);
            if (isSucceeded != expectSucceeded)
                throw new Exception(string.Format("Expected return is {0}, but actual return is {1}", expectSucceeded, isSucceeded));

            if (false == isSucceeded && resultWorkflow != null)
            {
                throw new Exception("TryConvertReference fails, but has a non-null workflow result");
            }
            else if (true == isSucceeded && resultWorkflow == null)
            {
                throw new Exception("TryConvertReference succeeded, but has a null workflow result");
            }

            return resultWorkflow;
        }

        public static void ValidateException(Exception expectedException, Exception actualException)
        {
            if ((expectedException == null) && (actualException == null))
            {
                return;
            }
            if ((expectedException == null) ^ (actualException == null))
            {
                //Log.TraceInternal("Expected exception: {0}\n", expectedException == null ? "null" : expectedException.ToString());
                //Log.TraceInternal("Actual exception: {0}\n", actualException == null ? "null" : actualException.ToString());
                throw new Exception("Actual exception and expected exception do not match!");
            }

            //Log.TraceInternal(string.Format("Expected exception: {0}", expectedException.ToString()));
            //Log.TraceInternal(string.Format("Actual exception: {0}", actualException.ToString()));
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                {"Message", actualException.Message}
            };

            // throws when validation fails.
            ExceptionHelpers.ValidateException(expectedException, actualException.GetType(), dict);
        }

        private static string ExecuteWorkflowAndGetResult(Activity we, out Exception ex)
        {
            ex = null;

            using (StreamWriter streamWriter = ConsoleSetOut())
            {
                using (TestWorkflowRuntime twr = new TestWorkflowRuntime(new TestWrapActivity(we)))
                {
                    twr.ExecuteWorkflow();
                    try
                    {
                        twr.WaitForCompletion(false);
                    }
                    // Capture ApplicationException only, which is thrown by TestWorkflowRuntime
                    catch (Exception e) // jasonv - approved; specific, commented, returns exception
                    {
                        ex = e.InnerException;
                    }
                }
            }

            if (!File.Exists(s_path))
            {
                throw new FileNotFoundException();
            }

            string[] texts = File.ReadAllLines(s_path);

            return string.Concat(texts);
        }

        private static StreamWriter ConsoleSetOut()
        {
            //StreamWriter streamWriter = PartialTrustStreamWriter.CreateStreamWriter(path);
            //streamWriter.AutoFlush = true;
            //PartialTrustConsole.SetOut(streamWriter);

            //return streamWriter;
            return null;
        }

        private static void RemoveAllWriteLinesFromSequence(Sequence sequence)
        {
            for (int i = sequence.Activities.Count - 1; i >= 0; i--)
            {
                Activity activity = sequence.Activities[i];

                if (activity is WriteLine)
                {
                    sequence.Activities.RemoveAt(i);
                }
            }
        }
    }

    public class TestWrapActivity : TestActivity
    {
        public TestWrapActivity(Activity activity)
        {
            string displayName = activity.DisplayName;
            this.ProductActivity = activity;
            this.DisplayName = displayName;
        }
    }
}
