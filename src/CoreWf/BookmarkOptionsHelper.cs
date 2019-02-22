// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System.Activities.Internals;
    using System.ComponentModel;

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
                throw FxTrace.Exception.AsError(
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
