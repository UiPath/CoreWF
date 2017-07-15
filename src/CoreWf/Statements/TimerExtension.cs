// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace CoreWf.Statements
{
    public abstract class TimerExtension
    {
        protected TimerExtension()
        {
        }

        public void RegisterTimer(TimeSpan timeout, Bookmark bookmark)
        {
            this.OnRegisterTimer(timeout, bookmark);
        }

        public void CancelTimer(Bookmark bookmark)
        {
            this.OnCancelTimer(bookmark);
        }

        protected abstract void OnRegisterTimer(TimeSpan timeout, Bookmark bookmark);
        protected abstract void OnCancelTimer(Bookmark bookmark);
    }
}
