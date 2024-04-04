// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using LegacyTest.Test.Common.Configurers;

namespace LegacyTest.Test.Common.TestObjects.Runtime.Core
{
    /// <summary>
    /// The runnable interface is used to markup a deployment object which can be invoked in another app domain. 
    ///  Run should always be called after the deployment has completed. Dispose will stop execution.
    /// 
    /// The interface doesnt define a way to determine when the work has completed, this is left as an implementation detail.
    /// 
    ///  *Note* Any class which extends from this should be // [Serializable] since it will be called cross appdomain.
    /// </summary>
    public interface IRunnable : IDisposable
    {
        /// <summary>
        /// Get the Settings for this runnable object.
        /// </summary>
        TestSettings Settings
        {
            get;
        }

        /// <summary>
        /// The run method will be invoked in the remote appdomain 
        /// </summary>
        void Run();

        // We cleanup in dispose
    }
}
