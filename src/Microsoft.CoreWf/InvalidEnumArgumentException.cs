// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.ComponentModel
{
    internal class InvalidEnumArgumentException : Exception
    {
        private string _argumentName;
        private int _direction;
        private Type _type;

        public InvalidEnumArgumentException()
        {
        }

        public InvalidEnumArgumentException(string message) : base(message)
        {
        }

        public InvalidEnumArgumentException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public InvalidEnumArgumentException(string argumentName, int direction, Type type)
        {
            _argumentName = argumentName;
            _direction = direction;
            _type = type;
        }
    }
}
