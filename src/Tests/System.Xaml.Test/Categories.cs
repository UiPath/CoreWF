using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoTests.System.Xaml
{
    static class Categories
    {
        /// <summary>
        /// Test does not work with System.Xaml from MS.NET (usually purposefully)
        /// </summary>
        public const string NotOnSystemXaml = "NotOnSystemXaml";

        /// <summary>
        /// Test should work but is not yet passing
        /// </summary>
        public const string NotWorking = "NotWorking";
    }
}
