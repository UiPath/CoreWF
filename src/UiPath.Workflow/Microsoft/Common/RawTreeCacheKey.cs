namespace Microsoft.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    class RawTreeCacheKey
    {
        static readonly IEqualityComparer<HashSet<Assembly>> AssemblySetEqualityComparer = HashSet<Assembly>.CreateSetComparer();
        static readonly IEqualityComparer<HashSet<string>> NamespaceSetEqualityComparer = HashSet<string>.CreateSetComparer();
        readonly string expressionText;
        readonly Type returnType;
        readonly HashSet<Assembly> assemblies;
        readonly HashSet<string> namespaces;

        readonly int hashCode;

        public RawTreeCacheKey(string expressionText, Type returnType, HashSet<Assembly> assemblies, IReadOnlyCollection<string> namespaces)
        {
            this.expressionText = expressionText;
            this.returnType = returnType;
            this.assemblies = new HashSet<Assembly>(assemblies);
            this.namespaces = new HashSet<string>(namespaces);

            hashCode = expressionText != null ? expressionText.GetHashCode() : 0;
            hashCode = CombineHashCodes(hashCode, AssemblySetEqualityComparer.GetHashCode(assemblies));
            hashCode = CombineHashCodes(hashCode, NamespaceSetEqualityComparer.GetHashCode(this.namespaces));
            if (returnType != null)
            {
                hashCode = CombineHashCodes(hashCode, returnType.GetHashCode());
            }
        }

        public override bool Equals(object obj)
        {
            RawTreeCacheKey rtcKey = obj as RawTreeCacheKey;
            if (rtcKey == null || hashCode != rtcKey.hashCode)
            {
                return false;
            }
            return expressionText == rtcKey.expressionText &&
                returnType == rtcKey.returnType &&
                AssemblySetEqualityComparer.Equals(assemblies, rtcKey.assemblies) &&
                NamespaceSetEqualityComparer.Equals(namespaces, rtcKey.namespaces);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        static int CombineHashCodes(int h1, int h2)
        {
            return ((h1 << 5) + h1) ^ h2;
        }
    }


    // this is a place holder for LambdaExpression(raw Expression Tree) that is to be stored in the cache
    // this wrapper is necessary because HopperCache requires that once you already have a key along with its associated value in the cache
    // you cannot add the same key with a different value.
    class RawTreeCacheValueWrapper
    {
        public LambdaExpression Value { get; set; }
    }

}
