// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;

namespace CoreWf
{
    [DataContract]
    public class NoPersistHandle : Handle
    {
        public NoPersistHandle()
        {
        }

        public void Enter(NativeActivityContext context)
        {
            context.ThrowIfDisposed();
            ThrowIfUninitialized();

            context.EnterNoPersist(this);
        }

        public void Exit(NativeActivityContext context)
        {
            context.ThrowIfDisposed();
            ThrowIfUninitialized();

            context.ExitNoPersist(this);
        }
    }
}


