
// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace System.Activities;

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
