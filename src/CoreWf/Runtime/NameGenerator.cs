// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Threading;

namespace System.Activities.Runtime
{
    internal class NameGenerator
    {
        private static NameGenerator s_nameGenerator = new NameGenerator();
        private long _id;
        private string _prefix;

        private NameGenerator()
        {
            _prefix = string.Concat("_", Guid.NewGuid().ToString().Replace('-', '_'), "_");
        }

        public static string Next()
        {
            long nextId = Interlocked.Increment(ref s_nameGenerator._id);
            return s_nameGenerator._prefix + nextId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
