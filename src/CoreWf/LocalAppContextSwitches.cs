// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

#pragma warning disable 0436 //Disable the type conflict warning for the types used by LocalAppContext framework due to InternalsVisibleTo for System.ServiceModel.Internals (Quirking)
namespace System.Activities
{
    using System;
    using System.Runtime.CompilerServices;

    internal static class LocalAppContextSwitches
    {
        //private static int useMD5ForWFDebugger;
        
        public static bool UseMD5ForWFDebugger
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return false;
                    //LocalAppContext.GetCachedSwitchValue(@"Switch.System.Activities.UseMD5ForWFDebugger", ref useMD5ForWFDebugger);
            }
        }
    }
}
