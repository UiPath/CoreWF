
namespace Microsoft.Common
{
    using System;
    using System.Activities.Runtime;
    using System.Security;

    [Fx.Tag.SecurityNote(Critical = "Critical because it holds a HostedCompiler instance, which requires FullTrust.")]
    [SecurityCritical]
    class HostedCompilerWrapper
    {
        readonly object wrapperLock;
        bool isCached;
        int refCount;

        public HostedCompilerWrapper(JustInTimeCompiler compiler)
        {
            Fx.Assert(compiler != null, "HostedCompilerWrapper must be assigned a non-null compiler");
            wrapperLock = new object();
            Compiler = compiler;
            isCached = true;
            refCount = 0;
        }

        public JustInTimeCompiler Compiler { get; private set; }

        // Storing ticks of the time it last used.
        public ulong Timestamp { get; private set; }

        // this is called only when this Wrapper is being kicked out the Cache
        public void MarkAsKickedOut()
        {
            IDisposable compilerToDispose = null;
            lock (wrapperLock)
            {
                isCached = false;
                if (refCount == 0)
                {
                    // if conditions are met,
                    // Dispose the HostedCompiler
                    compilerToDispose = Compiler as IDisposable;
                    Compiler = null;
                }
            }
            compilerToDispose?.Dispose();
        }

        // this always precedes Compiler.CompileExpression() operation in a thread of execution
        // this must never be called after Compiler.Dispose() either in MarkAsKickedOut() or Release()
        public void Reserve(ulong timestamp)
        {
            Fx.Assert(isCached, "Can only reserve cached HostedCompiler");
            lock (wrapperLock)
            {
                refCount++;
            }
            Timestamp = timestamp;
        }

        // Compiler.CompileExpression() is always followed by this in a thread of execution
        public void Release()
        {
            IDisposable compilerToDispose = null;
            lock (wrapperLock)
            {
                refCount--;
                if (!isCached && refCount == 0)
                {
                    // if conditions are met,
                    // Dispose the HostedCompiler
                    compilerToDispose = Compiler as IDisposable;
                    Compiler = null;
                }
            }
            compilerToDispose?.Dispose();
        }
    }
}
