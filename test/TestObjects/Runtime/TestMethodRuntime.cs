// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

namespace Test.Common.TestObjects.Runtime
{
    public class TestMethodRuntime : IDisposable
    {
        private string _assemblyQualifiedMethodTypeName;
        private string _methodName;

        internal TestMethodRuntime(Type methodType, string methodName)
        {
            if (methodType == null)
            {
                throw new ArgumentNullException("methodType");
            }

            if (methodName == null)
            {
                throw new ArgumentNullException("methodName");
            }

            _assemblyQualifiedMethodTypeName = methodType.AssemblyQualifiedName;
            _methodName = methodName;
        }

        public Type MethodType
        {
            get
            {
                return Type.GetType(_assemblyQualifiedMethodTypeName);
            }
            set { _assemblyQualifiedMethodTypeName = value.AssemblyQualifiedName; }
        }

        public string MethodName
        {
            get { return _methodName; }
            set { _methodName = value; }
        }

        public void ExecuteMethod()
        {
            // invoke the method using reflection //
            Type methodType = Type.GetType(_assemblyQualifiedMethodTypeName);

            if (methodType == null)
            {
                throw new Exception("No Type with given name found");
            }

            MemberInfo[] members = methodType.GetMember(_methodName, BindingFlags.Static | BindingFlags.Public);

            if (members.Length == 0)
            {
                throw new Exception(
                    String.Format("No public static method with name {0} found on type {1}",
                    _methodName, _assemblyQualifiedMethodTypeName));
            }

            if (members.Length > 1)
            {
                throw new Exception(
                    String.Format("More than one public static method with name {0} found on type {1}",
                    _methodName, _assemblyQualifiedMethodTypeName));
            }

            //methodType.InvokeMember(this.methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, null);
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion
    }
}
