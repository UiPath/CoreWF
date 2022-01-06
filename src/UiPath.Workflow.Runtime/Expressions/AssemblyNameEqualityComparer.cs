// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace System.Activities.Expressions;

internal class AssemblyNameEqualityComparer : IEqualityComparer, IEqualityComparer<AssemblyName>
{
    public AssemblyNameEqualityComparer() { }

    public new bool Equals(object xparam, object yparam)
    {
        if (xparam == null && yparam == null)
        {
            return true;
        }
        return Equals(xparam as AssemblyName, yparam as AssemblyName);
    }

    public bool Equals(AssemblyName x, AssemblyName y)
    {
        // this expects non-null AssemblyName
        if (x == null || y == null)
        {
            return false;
        }

        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x.Name != null && y.Name != null)
        {
            if (string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }
        }
        else if (!(x.Name == null && y.Name == null))
        {
            return false;
        }

        if (x.Version != null && y.Version != null)
        {
            if (x.Version != y.Version)
            {
                return false;
            }
        }
        else if (!(x.Version == null && y.Version == null))
        {
            return false;
        }

        if (x.CultureInfo != null && y.CultureInfo != null)
        {
            if (!x.CultureInfo.Equals(y.CultureInfo))
            {
                return false;
            }
        }
        else if (!(x.CultureInfo == null && y.CultureInfo == null))
        {
            return false;
        }

        byte[] xArray = x.GetPublicKeyToken();
        byte[] yArray = y.GetPublicKeyToken();
        if (!IsSameKeyToken(xArray, yArray))
        {
            return false;
        }

        return true;
    }

    public int GetHashCode(object objparam)
    {
        if (objparam is not AssemblyName obj)
        {
            return 0;
        }
        return GetHashCode(obj);
    }

    public int GetHashCode(AssemblyName obj)
    {
        int hashcode = 0;

        if (obj.Name != null)
        {
            hashcode ^= obj.Name.GetHashCode();
        }

        if (obj.Version != null)
        {
            hashcode ^= obj.Version.GetHashCode();
        }

        if (obj.CultureInfo != null)
        {
            hashcode ^= obj.CultureInfo.GetHashCode();
        }

        byte[] objArray = obj.GetPublicKeyToken();
        if (objArray != null)
        {
            // distinguishing no PKToken from "PKToken = null" which is an array of length=0
            hashcode ^= objArray.Length.GetHashCode() + 1;
            if (objArray.Length > 0)
            {
                hashcode ^= BitConverter.ToUInt64(objArray, 0).GetHashCode();
            }
        }
        return hashcode;
    }

    public static bool IsSameKeyToken(byte[] reqKeyToken, byte[] curKeyToken)
    {
        bool isSame = false;

        if (reqKeyToken == null && curKeyToken == null)
        {
            // Both Key Tokens are not set, treat them as same.
            isSame = true;
        }
        else if (reqKeyToken != null && curKeyToken != null)
        {
            // Both KeyTokens are set.
            if (reqKeyToken.Length == curKeyToken.Length)
            {
                isSame = true;
                for (int i = 0; i < reqKeyToken.Length; i++)
                {
                    if (reqKeyToken[i] != curKeyToken[i])
                    {
                        isSame = false;
                        break;
                    }
                }
            }
        }

        return isSame;
    }
}
