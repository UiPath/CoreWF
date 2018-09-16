// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using System;
    using CoreWf;
    using CoreWf.Internals;

    public sealed class Persist : NativeActivity
    {
        private static BookmarkCallback onPersistCompleteCallback;

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (context.IsInNoPersistScope)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPersistInsideNoPersist));
            }

            if (onPersistCompleteCallback == null)
            {
                onPersistCompleteCallback = new BookmarkCallback(OnPersistComplete);
            }

            context.RequestPersist(onPersistCompleteCallback);
        }

        private static void OnPersistComplete(NativeActivityContext context, Bookmark bookmark, object value)
        {
            // No-op.  This is here to keep the activity from completing.
        }
    }
}
