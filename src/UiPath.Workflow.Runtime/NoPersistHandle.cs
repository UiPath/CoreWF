// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

[DataContract]
public class NoPersistHandle : Handle
{
    public NoPersistHandle() { }

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
