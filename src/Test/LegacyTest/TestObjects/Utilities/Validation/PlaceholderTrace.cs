// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace LegacyTest.Test.Common.TestObjects.Utilities.Validation
{
    public class PlaceholderTrace : WorkflowTraceStep, IPlaceholderTraceProvider
    {
        public PlaceholderTrace()
        {
        }

        public PlaceholderTrace(IPlaceholderTraceProvider provider)
        {
            this.Provider = provider;
        }
        protected IPlaceholderTraceProvider Provider { get; set; }

        public virtual TraceGroup GetPlaceholderTrace()
        {
            return this.Provider.GetPlaceholderTrace();
        }
    }

    public interface IPlaceholderTraceProvider
    {
        TraceGroup GetPlaceholderTrace();
    }
}
