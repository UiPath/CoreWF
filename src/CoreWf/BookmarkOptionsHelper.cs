// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace CoreWf
{
    internal static class BookmarkOptionsHelper
    {
        private static bool IsDefined(BookmarkOptions options)
        {
            return options == BookmarkOptions.None || ((options & (BookmarkOptions.MultipleResume | BookmarkOptions.NonBlocking)) == options);
        }

        public static void Validate(BookmarkOptions options, string argumentName)
        {
            if (!IsDefined(options))
            {
                throw CoreWf.Internals.FxTrace.Exception.AsError(
                    new InvalidEnumArgumentException(argumentName, (int)options, typeof(BookmarkOptions)));
            }
        }

        public static bool SupportsMultipleResumes(BookmarkOptions options)
        {
            return (options & BookmarkOptions.MultipleResume) == BookmarkOptions.MultipleResume;
        }

        public static bool IsNonBlocking(BookmarkOptions options)
        {
            return (options & BookmarkOptions.NonBlocking) == BookmarkOptions.NonBlocking;
        }
    }
}
