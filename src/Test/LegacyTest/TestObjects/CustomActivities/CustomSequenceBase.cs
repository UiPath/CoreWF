// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Activities;

namespace LegacyTest.Test.Common.TestObjects.CustomActivities
{
    // Allows variable declaration and activity scheduling, as well as extension points (OnSequenceComplete and Execute)
    // for other test code to hook into. Based on System.Activities.Sequence.
    //[ContentProperty("Activities")]    
    public class CustomSequenceBase : NativeActivity
    {
        private readonly Collection<Activity> _activities;
        private readonly Collection<Variable> _variables;
        private Variable<int> _lastIndexHint;
        private CompletionCallback _onChildComplete;

        public CustomSequenceBase()
            : base()
        {
            _activities = new Collection<Activity>();
            _variables = new Collection<Variable>();
            _lastIndexHint = new Variable<int>();
        }

        public Collection<Variable> Variables
        {
            get
            {
                return _variables;
            }
        }

        public Collection<Activity> Activities
        {
            get
            {
                return _activities;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.SetChildrenCollection(this.Activities);
            metadata.SetVariablesCollection(this.Variables);
            metadata.AddImplementationVariable(_lastIndexHint);
        }

        protected override void Execute(NativeActivityContext executionContext)
        {
            if (this.Activities.Count > 0)
            {
                _onChildComplete = new CompletionCallback(InternalExecute);

                Activity nextChild = this.Activities[0];

                executionContext.ScheduleActivity(nextChild, _onChildComplete);
            }
            else
            {
                OnSequenceComplete(executionContext);
            }
        }

        protected virtual void OnSequenceComplete(NativeActivityContext executionContext)
        {
        }

        private void InternalExecute(NativeActivityContext executionContext, ActivityInstance completedInstance)
        {
            int completedInstanceIndex = _lastIndexHint.Get(executionContext);

            if (completedInstanceIndex >= this.Activities.Count || this.Activities[completedInstanceIndex] != completedInstance.Activity)
            {
                completedInstanceIndex = this.Activities.IndexOf(completedInstance.Activity);
            }

            int nextChildIndex = completedInstanceIndex + 1;

            if (nextChildIndex == this.Activities.Count)
            {
                OnSequenceComplete(executionContext);
                return;
            }

            if (_onChildComplete == null)
            {
                _onChildComplete = new CompletionCallback(InternalExecute);
            }

            Activity nextChild = this.Activities[nextChildIndex];

            executionContext.ScheduleActivity(nextChild, _onChildComplete);

            _lastIndexHint.Set(executionContext, nextChildIndex);
        }
    }
}
