// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Common.TestObjects.Utilities.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml;

    #region WorkflowTraceStep

#if !SILVERLIGHT
    [DataContract]
#endif
    public abstract class WorkflowTraceStep
    {
        private bool _optional = false;


        public bool Optional
        {
            get { return _optional; }
            set { _optional = value; }
        }


        internal bool Async
        {
            get { return false; } // keep it, in case it's needed later (validation engine can handle async steps)
            set
            {
            }
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(this.GetType().Name);

            WriteInnerXml(writer);

            writer.WriteEndElement();
        }

        protected virtual void WriteInnerXml(XmlWriter writer)
        {
            if (this.Async)
            {
                writer.WriteAttributeString("async", this.Async.ToString());
            }

            if (_optional)
            {
                writer.WriteAttributeString("optional", _optional.ToString());
            }
        }
    }
    #endregion

    #region IActualTraceStep
    public interface IActualTraceStep
    {
        DateTime TimeStamp { get; set; }
        int Validated { get; set; }
        bool Equals(IActualTraceStep trace);
        string GetStringId();
    }
    #endregion

    #region Trace groups
    [DataContract]
    public abstract class TraceGroup : WorkflowTraceStep
    {
        internal bool ordered;
        internal TraceGroup parent = null;
        internal int indexInParent = -1;

        internal int startIndex = -1;
        internal int[] endIndexes;
        private List<WorkflowTraceStep> _steps;

        protected TraceGroup(WorkflowTraceStep[] steps, bool ordered)
        {
            _steps = new List<WorkflowTraceStep>(steps);
            this.ordered = ordered;
        }

        public List<WorkflowTraceStep> Steps
        {
            get { return _steps; }
        }

        protected override void WriteInnerXml(XmlWriter writer)
        {
            base.WriteInnerXml(writer);

            foreach (WorkflowTraceStep trace in _steps)
            {
                trace.WriteXml(writer);
            }
        }

        //makes a copy of the existing trace group
        public static TraceGroup GetNewTraceGroup(TraceGroup traceGroup)
        {
            TraceGroup newTraceGroup = null;
            if (traceGroup.ordered)
            {
                newTraceGroup = new OrderedTraces();
            }
            else
            {
                newTraceGroup = new UnorderedTraces();
            }

            foreach (WorkflowTraceStep step in traceGroup.Steps)
            {
                ActivityTrace activityTrace = step as ActivityTrace;
                if (activityTrace != null)
                {
                    newTraceGroup.Steps.Add(new ActivityTrace(activityTrace) { Optional = step.Optional });

                    continue;
                }

                WorkflowInstanceTrace workflowInstanceTrace = step as WorkflowInstanceTrace;
                if (workflowInstanceTrace != null)
                {
                    newTraceGroup.Steps.Add(new WorkflowInstanceTrace(workflowInstanceTrace.InstanceName, workflowInstanceTrace.InstanceStatus) { Optional = step.Optional });
                    continue;
                }

                UserTrace userTrace = step as UserTrace;
                if (userTrace != null)
                {
                    newTraceGroup.Steps.Add(new UserTrace(userTrace.InstanceId, userTrace.ActivityParent, userTrace.Message));
                    continue;
                }

                BookmarkResumptionTrace bookmarkResumptionTrace = step as BookmarkResumptionTrace;
                if (bookmarkResumptionTrace != null)
                {
                    newTraceGroup.Steps.Add(new BookmarkResumptionTrace(bookmarkResumptionTrace.BookmarkName,
                        bookmarkResumptionTrace.SubinstanceId, bookmarkResumptionTrace.ActivityName));
                    continue;
                }

                SynchronizeTrace synchronizeTrace = step as SynchronizeTrace;
                if (synchronizeTrace != null)
                {
                    newTraceGroup.Steps.Add(new SynchronizeTrace(synchronizeTrace.userTrace.InstanceId,
                        synchronizeTrace.userTrace.Message));
                    continue;
                }

                ActivityPlaceholderTrace activityPlaceholderTrace = step as ActivityPlaceholderTrace;
                if (activityPlaceholderTrace != null)
                {
                    newTraceGroup.Steps.Add(activityPlaceholderTrace);
                    continue;
                }

                TraceGroup tempTraceGroup = step as TraceGroup;
                if (tempTraceGroup != null)
                {
                    newTraceGroup.Steps.Add(TraceGroup.GetNewTraceGroup(tempTraceGroup));
                    continue;
                }
            }
            return newTraceGroup;
        }

        public override bool Equals(object obj)
        {
            TraceGroup traceGroup = obj as TraceGroup;
            if (traceGroup != null)
            {
                return (this.ToString() == traceGroup.ToString());
            }
            return base.Equals(obj);
        }


        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [DataContract]
    public class OrderedTraces : TraceGroup
    {
        public OrderedTraces(params WorkflowTraceStep[] steps)
            : base(steps, true)
        {
        }
    }

    [DataContract]
    public class UnorderedTraces : TraceGroup
    {
        public UnorderedTraces(params WorkflowTraceStep[] steps)
            : base(steps, false)
        {
        }
    }
    #endregion

    #region ValidationFailedException

    public class ValidationFailedException : Exception
    {
        public ValidationFailedException()
            : base()
        {
        }
    }
    #endregion
}
