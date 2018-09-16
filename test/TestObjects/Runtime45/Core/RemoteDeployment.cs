// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;

namespace Test.Common.TestObjects.Runtime.Core
{
    //using Microsoft.Infrastructure.Test;

    /// <summary>
    /// This class is used as the inbetween for cases where we are executing a runnable object in another app domain.
    ///  Instead of having each IRunnable be MarshalByRef, instead we just serialize the Runnable and the test settings to the
    ///  remote app domain, and then we call Run on it from the remote app domain.
    ///  
    /// Note that this object will always be invoked in the remote app-domain.
    /// </summary>
    // [Serializable]
    internal class RemoteDeployment : IDisposable
    {
        /// <summary>
        /// We keep this around so that we can call dispose on it.
        /// </summary>
        private IRunnable _runnable;

        /// <summary>
        /// This constructor is called in the cross app domain case, where we are marshalling the RemoteDeployment.
        /// Note that before you call into this, you need to set the Runnable and TestCaseCurrent in the Appdomain.
        /// 
        /// This will automatically call run.
        /// </summary>
        public RemoteDeployment()
        {
            // 
            // this.runnable = (IRunnable)AppDomain.CurrentDomain.GetData(TestDomainManager.RunnableKey);
            // TestCase.SetCurrent((TestCase)AppDomain.CurrentDomain.GetData(TestDomainManager.TestCaseCurrentKey));
            if (_runnable == null)
            {
                throw new ArgumentException("runtime");
            }
            _runnable.Run();
        }

        /// <summary>
        /// This constructor is called in the same app domain case, we just pass the Runnable as a parameter.
        /// </summary>
        /// <param name="runtime">The object that should be run.</param>
        public RemoteDeployment(IRunnable runtime)
        {
            _runnable = runtime;
            runtime.Run();
        }

        /// <summary>
        /// Need to cleanup the runnable object, which is done by contract in the dispose.
        /// </summary>
        public void Dispose()
        {
            //Log.Info("Called RemoteDeployment.Dispose()");

            if (_runnable != null)
            {
                _runnable.Dispose();
                _runnable = null;
            }
        }
    }
}
