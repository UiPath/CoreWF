// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Test.Common.TestObjects.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class ExceptionHelpers
    {
        public static void CheckForException(
            Type exceptionType,
            MethodDelegate tryCode)
        {
            ExceptionHelpers.CheckForException(
                exceptionType,
                new Dictionary<string, string>(),
                tryCode,
                false);
        }

        public static void CheckForException(Type exceptionType, string message, MethodDelegate tryCode)
        {
            ExceptionHelpers.CheckForException(exceptionType, message, tryCode, false);
        }

        public static void CheckForException(Type exceptionType, string message, MethodDelegate tryCode, bool checkValidationException)
        {
            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();
            exceptionProperties.Add("Message", message);
            ExceptionHelpers.CheckForException(exceptionType, exceptionProperties, tryCode, checkValidationException);
        }

        public static void CheckForException(
            Type exceptionType,
            Dictionary<string, string> exceptionProperties,
            MethodDelegate tryCode)
        {
            ExceptionHelpers.CheckForException(
                exceptionType,
                exceptionProperties,
                tryCode,
                false);
        }

        public static void CheckForException(
            Type exceptionType,
            Dictionary<string, string> exceptionProperties,
            MethodDelegate tryCode,
            bool checkValidationException)
        {
            if (exceptionType == typeof(System.Activities.ValidationException)
                && checkValidationException == false)
            {
                throw new Exception("Please do not use this method to check for ValidationExceptions and"
                            + "use \"TestRuntime.ValidateWorkflowErrors\" method instead.");
            }

            // The normal state for this method is for an exception to be thrown
            bool exceptionThrown = true;
            try
            {
                // call delegate
                tryCode();

                // We did not get an exception.  Normally we would throw here, but due to the
                // catch handler below we set the flag and throw outside the try block
                exceptionThrown = false;
            }
            catch (Exception exc) // jasonv - approved; delegate may throw any exception; we rethrow as inner in case of failure
            {
                ValidateException(exc, exceptionType, exceptionProperties);
            }

            if (!exceptionThrown)
            {
                throw new ValidateExceptionFailed(string.Format(
                    "Expected {0} to be thrown, but no exception was thrown.",
                    exceptionType.FullName));
            }
        }

        public static void CheckForInnerException(Type exceptionType, MethodDelegate tryCode)
        {
            ExceptionHelpers.CheckForInnerException(exceptionType, new Dictionary<string, string>(), tryCode);
        }

        public static void CheckForInnerException(Type exceptionType, string message, MethodDelegate tryCode)
        {
            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();
            exceptionProperties.Add("Message", message);

            ExceptionHelpers.CheckForInnerException(exceptionType, exceptionProperties, tryCode);
        }

        public static void CheckForInnerException(Type exceptionType, Dictionary<string, string> exceptionProperties, MethodDelegate tryCode)
        {
            // The normal state for this method is for an exception to be thrown
            bool exceptionThrown = true;
            try
            {
                // call delegate
                tryCode();

                // We did not get an exception.  Normally we would throw here, but due to the
                // catch handler below we set the flag and throw outside the try block
                exceptionThrown = false;
            }
            catch (Exception exc) // jasonv - approved; delegate may throw any exception; we rethrow as inner in case of failure
            {
                ValidateException(exc.InnerException, exceptionType, exceptionProperties);
            }

            if (!exceptionThrown)
            {
                throw new ValidateExceptionFailed(string.Format(
                    "Expected {0} to be thrown, but no exception was thrown.",
                    exceptionType.FullName));
            }
        }

        public static void SearchAndValidateException(Type exceptionType, MethodDelegate tryCode)
        {
            SearchAndValidateException(exceptionType, new Dictionary<string, string>(), tryCode);
        }

        public static void SearchAndValidateException(Type exceptionType, string message, MethodDelegate tryCode)
        {
            Dictionary<string, string> exceptionProperties = new Dictionary<string, string>();
            exceptionProperties.Add("Message", message);
            SearchAndValidateException(exceptionType, exceptionProperties, tryCode);
        }

        private static void SearchAndValidateException(Type exceptionType, Dictionary<string, string> exceptionProperties, MethodDelegate tryCode)
        {
            bool exceptionThrown = true;

            try
            {
                tryCode();
                exceptionThrown = false;
            }
            catch (Exception exception)
            {
                SearchStackForValidException(exceptionType, exception, exceptionProperties, out bool validationPassed, out Exception lastException);
            }

            if (!exceptionThrown)
            {
                throw new ValidateExceptionFailed(string.Format("Expected {0} to be thrown, but no exception was thrown.", exceptionType.FullName));
            }
        }

        public static void SearchStackForValidException(Type exceptionType, Exception actualException, Dictionary<string, string> exceptionProperties, out bool validationPassed, out Exception lastException)
        {
            validationPassed = false;
            lastException = null;

            do
            {
                try
                {
                    ValidateException(actualException, exceptionType, exceptionProperties);
                    validationPassed = true;
                }
                catch (Exception validationException)
                {
                    lastException = validationException;
                    actualException = actualException.InnerException;
                }
            }
            while
                (!validationPassed && actualException != null);
        }

        public static void CheckForTargetInvocationException(Type exceptionType, Dictionary<string, string> exceptionProperties, MethodDelegate tryCode)
        {
            // The normal state for this method is for an exception to be thrown
            bool exceptionThrown = true;
            try
            {
                // call delegate
                tryCode();

                // We did not get an exception.  Normally we would throw here, but due to the
                // catch handler below we set the flag and throw outside the try block
                exceptionThrown = false;
            }
            catch (TargetInvocationException ex) // jasonv - approved; delegate may throw any exception; we rethrow as inner in case of failure
            {
                //if (ex.InnerException != null && (ex.InnerException is FaultException<ExceptionDetail>))
                //{
                //    FaultException<ExceptionDetail> faultEx = (FaultException<ExceptionDetail>)ex.InnerException;
                //    ValidateFaultException(faultEx, exceptionType, exceptionProperties);
                //}
                //else
                //{
                //    throw ex;
                //}
            }

            if (!exceptionThrown)
            {
                throw new ValidateExceptionFailed(string.Format(
                    "Expected {0} to be thrown, but no exception was thrown.",
                    exceptionType.FullName));
            }
        }

        public static void ValidateException(
            Exception exception,
            Type exceptionType,
            Dictionary<string, string> exceptionProperties)
        {
            // check for exception type mismatch
            if (exception.GetType().FullName != exceptionType.FullName)
            {
                throw new ValidateExceptionFailed(String.Format(
                    "Expected {0} to be thrown, but {1} was thrown.",
                    exceptionType.FullName,
                    exception.GetType().FullName),
                    exception);
            }

            ValidateExceptionProperties(exception, exceptionProperties);

            //Log.TraceInternal("Exception was validated successfully");
        }

        public static void ValidateExceptionProperties(Exception exception, Dictionary<string, string> exceptionProperties)
        {
            var excTypeFullName = exception.GetType().FullName;

            if (exceptionProperties != null)
            {
                foreach (string propertyName in exceptionProperties.Keys)
                {
                    string expectedPropertyValue = exceptionProperties[propertyName];

                    PropertyInfo pi = exception.GetType().GetProperty(propertyName);

                    if (pi == null)
                    {
                        throw new Exception(String.Format(
                            "Test issue: {0} doesn't have a property {1}",
                            excTypeFullName,
                            propertyName),
                            exception);
                    }

                    object actualPropertyObjectValue = pi.GetValue(exception, null);

                    if (actualPropertyObjectValue == null)
                    {
                        throw new ValidateExceptionFailed(String.Format(
                            "Property {0} on {1} was expected to contain {2}. The actual value is null",
                            propertyName,
                            excTypeFullName,
                            expectedPropertyValue),
                            exception);
                    }

                    string actualPropertyValue = actualPropertyObjectValue.ToString();

                    if (!actualPropertyValue.Contains(expectedPropertyValue))
                    {
                        throw new ValidateExceptionFailed(String.Format(
                            "Property {0} on {1} was expected to contain {2}. The actual value is {3}",
                            propertyName,
                            excTypeFullName,
                            expectedPropertyValue,
                            actualPropertyValue),
                            exception);
                    }
                }
            }
        }

        //public static void ValidateFaultException(
        //    FaultException<ExceptionDetail> exception,
        //    Type exceptionType,
        //    Dictionary<string, string> exceptionProperties)
        //{
        //    ExceptionDetail detail = exception.Detail;
        //    // check for exception type mismatch
        //    if (detail.Type != exceptionType.FullName)
        //    {
        //        throw new ValidateExceptionFailed(String.Format(
        //            "Expected {0} to be thrown, but {1} was thrown.",
        //            exceptionType.FullName,
        //            detail.Type),
        //            exception);
        //    }

        //    // check for property values
        //    if (exceptionProperties != null)
        //    {
        //        foreach (string propertyName in exceptionProperties.Keys)
        //        {
        //            string expectedPropertyValue = exceptionProperties[propertyName];

        //            PropertyInfo pi = detail.GetType().GetProperty(propertyName);

        //            if (pi == null)
        //            {
        //                throw new Exception(String.Format(
        //                    "Test issue: {0} doesn't have a property {1}",
        //                    exceptionType.FullName,
        //                    propertyName),
        //                    exception);
        //            }

        //            object actualPropertyObjectValue = pi.GetValue(detail, null);

        //            if (actualPropertyObjectValue == null)
        //            {
        //                throw new ValidateExceptionFailed(String.Format(
        //                    "Property {0} on {1} was expected to contain {2}. The actual value is null",
        //                    propertyName,
        //                    exceptionType.FullName,
        //                    expectedPropertyValue),
        //                    exception);
        //            }

        //            string actualPropertyValue = actualPropertyObjectValue.ToString();

        //            if (!actualPropertyValue.Contains(expectedPropertyValue))
        //            {
        //                throw new ValidateExceptionFailed(String.Format(
        //                    "Property {0} on {1} was expected to contain {2}. The actual value is {3}",
        //                    propertyName,
        //                    exceptionType.FullName,
        //                    expectedPropertyValue,
        //                    actualPropertyValue),
        //                    exception);
        //            }
        //        }
        //    }

        //    //Log.TraceInternal("[ExceptionHelpers] Exception was validated successfully");
        //}

        /// <summary>
        /// Should be used when the test is blocked by the product.
        /// 
        /// Helper added because throwing an exception at the beginning of a test case will give you a build warning,
        /// unreachable code detected, which because of build settings will be an error.  To resolve this you need to 
        /// have code like following:
        /// 
        /// bool flag = true;
        /// 
        /// if (flag)
        ///     throw new Exception();
        /// 
        /// </summary>
        /// <param name="reason">The reason the test was blocked.</param>
        public static void ThrowTestCaseBlockedException(string reason)
        {
            throw new Exception(string.Format("TEST CASE BLOCKED - {0}", reason));
        }

        /// <summary>
        /// Should be used when the machine configuration does not included something
        /// required by the test case.  e.g. Sql or IIS not installed.
        /// 
        /// Helper added because throwing an exception at the beginning of a test case will give you a build warning,
        /// unreachable code detected, which because of build settings will be an error.  To resolve this you need to 
        /// have code like following:
        /// 
        /// bool flag = true;
        /// 
        /// if (flag)
        ///     throw new Exception();
        /// </summary>
        /// <param name="reason">The reason the test was skipped.</param>
        public static void ThrowTestCaseSkippedException(string reason)
        {
            //throw new TestCaseSkippedException(reason);

        }
        public delegate void MethodDelegate();

        /// <summary>
        /// This method is used for XUnit tests that anticipate an exception with a specified set of properties
        /// </summary>
        /// <typeparam name="ExceptionType"></typeparam>
        /// <param name="testCode"></param>
        /// <param name="exceptionProperties"></param>
        public static void Throws<ExceptionType>(Action testCode, Dictionary<string, string> exceptionProperties)
            where ExceptionType : Exception
        {
            try
            {
                testCode.Invoke();
                Xunit.Assert.True(false, string.Format("Expected exception {0} was not thrown.", typeof(ExceptionType).FullName));
            }
            catch (ExceptionType exception)
            {
                ValidateExceptionProperties(exception, exceptionProperties);
            }
            catch (Exception exception)
            {
                Xunit.Assert.True(false, string.Format(String.Format(
                    "Expected {0} to be thrown, but {1} was thrown.",
                    typeof(ExceptionType).FullName,
                    exception.GetType().FullName)));
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
     // [Serializable]
    public class ValidateExceptionFailed : Exception
    {
        // Public ctor for serialization
        public ValidateExceptionFailed()
        {
        }

        //protected ValidateExceptionFailed(SerializationInfo info, StreamingContext context) : base(info, context) 
        //{ 
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public ValidateExceptionFailed(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ValidateExceptionFailed(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
