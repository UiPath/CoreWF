// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using LegacyTest.Test.Common.TestObjects.Utilities.Validation;

namespace LegacyTest.Test.Common.TestObjects.Utilities
{
    public class TestTraceListenerExtension
    {
        public const string ListenerName = "LegacyTest.Test.Common.TestObjects.Utilities.Trace.TestTraceListener, LegacyTest.Test.Common.TestObjects";
        protected TestTraceManager testTraceManager;

        public TestTraceListenerExtension()
        {
            this.testTraceManager = GetTraceManager();
        }

        public void TraceData(object data)
        {
            //tracking data is now directly pushed by the tracking participant to the test trace manager.
            if (data is WorkflowInstanceTrace instanceTrace)
            {
                this.testTraceManager.AddTrace(instanceTrace.InstanceName, instanceTrace);
                //Log.TraceInternal("[TestTraceListener] {0}", instanceTrace.ToString());
            }
            else if (data is WorkflowExceptionTrace exceptionTrace)
            {
                this.testTraceManager.AddTrace(exceptionTrace.InstanceName, exceptionTrace);
                //Log.TraceInternal("[TestTraceListener] {0}", exceptionTrace.ToString());
            }
            else if (data is UserTrace userTrace)
            {
                this.testTraceManager.AddTrace(userTrace.InstanceId, userTrace);
                //Log.TraceInternal("[TestTraceListener] {0}", userTrace.ToString());
            }
            else if (data is SynchronizeTrace)
            {
                SynchronizeTrace synchronizeTrace = (SynchronizeTrace)data;
                this.testTraceManager.AddTrace(synchronizeTrace.userTrace.InstanceId, synchronizeTrace);
                //Log.TraceInternal("[TestTraceListener] {0}", synchronizeTrace.ToString());
            }
            else if (data is WorkflowAbortedTrace synchronizeTrace)
            {
                this.testTraceManager.AddTrace(synchronizeTrace.InstanceId, synchronizeTrace);
                //Log.TraceInternal("[TestTraceListener] {0}", synchronizeTrace.ToString());
            }
        }


        //public override void Write(string message)
        //{
        //    WriteLine(message);
        //}

        //public override void WriteLine(string message)
        //{
        //    Log.TraceInternal(message);
        //}

        //private bool TryConvertToEnum(string stateAsString, out ActivityInstanceState actualState)
        //{
        //    foreach (ActivityInstanceState state in Enum.GetValues(typeof(ActivityInstanceState)))
        //    {
        //        if (state.ToString() == stateAsString)
        //        {
        //            actualState = state;
        //            return true;
        //        }
        //    }

        //    // We must have had a custom state that was tracked
        //    actualState = default(ActivityInstanceState);
        //    return false;
        //}

        protected virtual TestTraceManager GetTraceManager()
        {
            return TestTraceManager.Instance;
        }
    }
}
