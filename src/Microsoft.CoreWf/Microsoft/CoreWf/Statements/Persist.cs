// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf.Statements
{
    public sealed class Persist : NativeActivity
    {
        private static BookmarkCallback s_onPersistCompleteCallback;

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
                throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotPersistInsideNoPersist));
            }

            if (s_onPersistCompleteCallback == null)
            {
                s_onPersistCompleteCallback = new BookmarkCallback(OnPersistComplete);
            }

            context.RequestPersist(s_onPersistCompleteCallback);
        }

        private static void OnPersistComplete(NativeActivityContext context, Bookmark bookmark, object value)
        {
            // No-op.  This is here to keep the activity from completing.
        }
    }
}
