// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Runtime
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class CompletionBookmark
    {
        private CompletionCallbackWrapper callbackWrapper;

        public CompletionBookmark()
        {
            // Called when we want to use the special completion callback
        }

        public CompletionBookmark(CompletionCallbackWrapper callbackWrapper)
        {
            this.callbackWrapper = callbackWrapper;
        }

        [DataMember(EmitDefaultValue = false, Name = "callbackWrapper")]
        internal CompletionCallbackWrapper SerializedCallbackWrapper
        {
            get { return this.callbackWrapper; }
            set { this.callbackWrapper = value; }
        }

        public void CheckForCancelation()
        {
            Fx.Assert(this.callbackWrapper != null, "We must have a callback wrapper if we are calling this.");
            this.callbackWrapper.CheckForCancelation();
        }

        public WorkItem GenerateWorkItem(ActivityInstance completedInstance, ActivityExecutor executor)
        {
            if (this.callbackWrapper != null)
            {
                return this.callbackWrapper.CreateWorkItem(completedInstance, executor);
            }
            else
            {
                // Variable defaults and argument expressions always have a parent
                // and never have a CompletionBookmark
                if (completedInstance.State != ActivityInstanceState.Closed && completedInstance.Parent.HasNotExecuted)
                {
                    completedInstance.Parent.SetInitializationIncomplete();
                }

                return new EmptyWithCancelationCheckWorkItem(completedInstance.Parent, completedInstance);
            }
        }
    }
}
