// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using LegacyTest.Test.Common.Configurers;

namespace LegacyTest.Test.Common.TestObjects.Runtime.Core
{
    /// <summary>
    /// The deployable interface is used to show that this object knows how to create the environment that its tasks need to be run in.
    ///  When Deploy is invoked it means that the object is ready, and you should setup whatever is needed for the environment.
    /// </summary>
    public interface IDeployable : IDisposable
    {
        /// <summary>
        /// Get the settings for the deployment.
        /// </summary>
        TestSettings Settings
        {
            get;
        }

        /// <summary>
        /// Deployment is basically doing all of the setup steps necessary to startup. This method will always be invoked in the original test app domain,
        ///  and is responsable for creating any additional app domains/etc. needed for the test.
        /// Note that it is up to the deploy method to invoke any IRunnable tasks that it requires to be executing.
        /// </summary>
        void Deploy();

        // We cleanup on dispose
        // Note that IDeployable is responsible for disposing of the TestSettings in its Dispose.
    }
}
