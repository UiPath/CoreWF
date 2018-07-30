// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using Test.Common.Configurers;
using Test.Common.TestObjects.Runtime.Core;

namespace Test.Common.TestObjects.Runtime45.Client
{
    /// <summary>
    /// Used to execute an arbitrary method in another app domain
    /// The method that is provided should have no return value, and should accept only one argument which is the TestSettings collection.
    /// This class handles both the deployment (Creating the remote app domain), and running (executing the method), so it will be passed from the 
    /// original app domain to the second, this means that it must be serializable.
    /// </summary>
    // [Serializable]
    public class ExecuteMethod : IDeployable, IRunnable
    {
        //private TestDomainManager deployment;
        private readonly string _assemblyQualifiedMethodTypeName;
        private readonly string _methodName;
        private TestSettings _settings;
        private bool _isDiposed = false;

        public TestSettings Settings
        {
            get
            {
                return _settings;
            }
        }


        /// <summary>
        /// This is private since you should always use the Execute helper.
        /// </summary>
        /// <param name="methodType">The type that the method is defined on.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="settings">The settings that should be used during execution.</param>
        private ExecuteMethod(Type methodType, string methodName, TestSettings settings)
        {
            if (methodType == null)
            {
                throw new ArgumentNullException("methodType");
            }

            // We extract the type name from the type that is passed in, since type cant be serialized
            _assemblyQualifiedMethodTypeName = methodType.AssemblyQualifiedName;
            _methodName = methodName ?? throw new ArgumentNullException("methodName");
            _settings = settings;
        }

        /// <summary>
        /// Static helper which is used to execute a method in the remote app domain.
        /// This wraps calling deploy and dispose.
        /// 
        /// This will invoke all IConfigurer<Configuration> in order to modify the config file that gets generated.
        /// </summary>
        /// <param name="methodType">The type that the method is defined on.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="settings">The settings that should be used during execution.</param>
        public static void Execute(Type methodType, string methodName, TestSettings settings)
        {
            using (ExecuteMethod em = new ExecuteMethod(methodType, methodName, settings))
            {
                em.Deploy();
            }
        }

        /// <summary>
        /// Deployment just uses the TestDomainManager to create a new app domain for this test.
        /// </summary>
        public void Deploy()
        {
            //if (this.deployment != null)
            //{
            //    throw new InvalidOperationException("Attempting to deploy the same ExecuteMethod multiple times!");
            //}

            //this.deployment = TestDomainManager.CreateTestEnvironment(this);
        }

        /// <summary>
        /// Invokes the method in the remote app domain. 
        /// 
        /// Note that this will work under partial trust because we only allow calls to public methods.
        /// </summary>
        public void Run()
        {
            Type methodType = Type.GetType(_assemblyQualifiedMethodTypeName);
            if (methodType == null)
            {
                throw new Exception(string.Format("Type {0} could not be found", _assemblyQualifiedMethodTypeName));
            }

            object[] args = new object[1]
            {
                this.Settings
            };

            //methodType.InvokeMember(this.methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, args);
        }

        /// <summary>
        /// The Deployable object is responsable for cleaning up the TestSettings, and we need to clean up the deployment 
        /// so that we unload the app domain.
        /// </summary>
        public void Dispose()
        {
            if (!_isDiposed)
            {
                // Because this is both runnable and deployable, we need to protected ourselves because we will call dispose twice
                _isDiposed = true;

                if (_settings != null)
                {
                    _settings.Dispose();
                }
                //if (this.deployment != null)
                //{
                //    this.deployment.Dispose();
                //}
            }
        }
    }
}
