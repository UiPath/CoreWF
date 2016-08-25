// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Test.Common.TestObjects.Utilities
{
    /// <summary>
    /// Helper class used to perform parameter validation
    /// </summary>
    public sealed class CheckParameter
    {
        private CheckParameter() { }

        /// <summary>
        /// Throws an ArgumentNullException if the "param" is null
        /// </summary>
        /// <param name="param">the object to validate is not null</param>
        /// <param name="name">The name of this parameter</param>
        public static void NotNull(object param, string name)
        {
            if (null == param)
                throw new ArgumentNullException(name);
        }

        /// <summary>
        /// Throws an ArgumentNullException if the "param" is null
        /// </summary>
        /// <param name="param">the object to validate is not null</param>
        public static void NotNull(object param)
        {
            if (null == param)
                throw new ArgumentNullException("param", "Parameter value should not be null");
        }

        /// <summary>
        /// Throws an exception if this string is null or zero-length
        /// </summary>
        /// <param name="param">The string to check</param>
        /// <param name="paramName">the name of the parameter</param>
        public static void ValidString(string param, string paramName)
        {
            if (null == param)
                throw new ArgumentNullException(paramName);
            if (param.Length == 0)
                throw new ArgumentException(paramName + " must contain a value");
        }

        /// <summary>
        /// Throws an ApplicationException if the boolean condition is false.
        /// </summary>
        /// <param name="test">the condition to test, false causes an exception to be thrown</param>
        /// <param name="message">The message to pass if condition failed</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public static void Assert(bool test, string message)
        {
            //raising application exception since this piece of code is used in a lot of places.
            //Also throwing of ApplicationException is not harmful but purely bad.
            if (!test)
                throw new Exception(message);
        }

        /// <summary>
        /// Throws file not found exception if file does not exist
        /// </summary>
        /// <param name="filePath"></param>
        /// <exception cref="FileNotFoundException"></exception>
        public static void ValidFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);
        }

        /// <summary>
        /// Throws directory not found exception if directory does not exist
        /// </summary>
        public static void ValidDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(directoryPath);
        }

        /// <summary>
        /// Throws file not found exception if file or directory does not exist
        /// </summary>
        public static void ValidFileOrDirectory(string path)
        {
            if (!System.IO.File.Exists(path) && !Directory.Exists(path))
                throw new System.IO.FileNotFoundException("File or Directory not Found " + path);
        }
    }
}
