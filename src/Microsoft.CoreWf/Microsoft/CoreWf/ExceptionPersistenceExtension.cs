// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CoreWf
{
    public class ExceptionPersistenceExtension
    {
        private bool _persistExceptions;

        public ExceptionPersistenceExtension()
        {
            _persistExceptions = true;
        }

        public bool PersistExceptions
        {
            get
            {
                return _persistExceptions;
            }
            set
            {
                _persistExceptions = value;
            }
        }
    }
}
