// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace LegacyLegacyTest.Test.Common
{
    public class CustomException : Exception
    {
        public CustomException()
            : base()
        {
        }

        public CustomException(string message)
            : base(message)
        {
        }

        public string Name
        {
            get;
            set;
        }
    }
}
