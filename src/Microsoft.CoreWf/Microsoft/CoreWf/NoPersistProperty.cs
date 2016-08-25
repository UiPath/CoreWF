// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;

namespace Microsoft.CoreWf
{
    // This does not need to be data contract since we'll never persist while one of these is active
    internal class NoPersistProperty : IPropertyRegistrationCallback
    {
        public const string Name = "Microsoft.CoreWf.NoPersistProperty";

        private ActivityExecutor _executor;
        private int _refCount;

        public NoPersistProperty(ActivityExecutor executor)
        {
            _executor = executor;
        }

        public void Enter()
        {
            _refCount++;
            _executor.EnterNoPersist();
        }

        public bool Exit()
        {
            Fx.Assert(_refCount > 0, "We should guard against too many exits elsewhere.");

            _refCount--;
            _executor.ExitNoPersist();

            return _refCount == 0;
        }

        public void Register(RegistrationContext context)
        {
        }

        public void Unregister(RegistrationContext context)
        {
            if (_refCount > 0)
            {
                for (int i = 0; i < _refCount; i++)
                {
                    _executor.ExitNoPersist();
                }

                _refCount = 0;
            }
        }
    }
}


