// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Runtime;

    // This does not need to be data contract since we'll never persist while one of these is active
    internal class NoPersistProperty : IPropertyRegistrationCallback
    {
        public const string Name = "System.Activities.NoPersistProperty";
        private readonly ActivityExecutor executor;
        private int refCount;

        public NoPersistProperty(ActivityExecutor executor)
        {
            this.executor = executor;
        }

        public void Enter()
        {
            this.refCount++;
            this.executor.EnterNoPersist();
        }

        public bool Exit()
        {
            Fx.Assert(this.refCount > 0, "We should guard against too many exits elsewhere.");

            this.refCount--;
            this.executor.ExitNoPersist();

            return this.refCount == 0;
        }

        public void Register(RegistrationContext context)
        {
        }

        public void Unregister(RegistrationContext context)
        {
            if (this.refCount > 0)
            {
                for (int i = 0; i < this.refCount; i++)
                {
                    this.executor.ExitNoPersist();
                }

                this.refCount = 0;
            }
        }
    }
}


