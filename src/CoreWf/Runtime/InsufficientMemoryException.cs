// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace CoreWf.Runtime
{
    internal class InsufficientMemoryException : Exception
    {
        public InsufficientMemoryException()
        {
        }
        public InsufficientMemoryException(string message) : base(message)
        {
        }

        public InsufficientMemoryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
