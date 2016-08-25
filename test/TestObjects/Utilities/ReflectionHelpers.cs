// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace Test.Common.TestObjects.Utilities
{
    public static class ReflectionHelpers
    {
        static public string GetFullAssemblyNameSpace(string nameSpace, string assemblyNameSpace)
        {
            return string.Format("{0}, {1}", nameSpace, GetFullAssemblyNameSpace(assemblyNameSpace));
        }

        static public string GetFullAssemblyNameSpace(string assembly)
        {
            //if (Path.GetPathRoot(assembly) == string.Empty)
            //{
            //    assembly = Path.Combine(DirectoryAssistance.GetDotNetFrameworkFolder(), assembly);
            //}

            //return Assembly.ReflectionOnlyLoadFrom(assembly).ToString();
            return string.Empty;
        }
    }
}
