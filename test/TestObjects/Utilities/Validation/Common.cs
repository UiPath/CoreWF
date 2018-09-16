// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

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
        private readonly List<WorkflowTraceStep> _steps;

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
                if (step is ActivityTrace activityTrace)
                {
                    newTraceGroup.Steps.Add(new ActivityTrace(activityTrace) { Optional = step.Optional });

                    continue;
                }

                if (step is WorkflowInstanceTrace workflowInstanceTrace)
                {
                    newTraceGroup.Steps.Add(new WorkflowInstanceTrace(workflowInstanceTrace.InstanceName, workflowInstanceTrace.InstanceStatus) { Optional = step.Optional });
                    continue;
                }

                if (step is UserTrace userTrace)
                {
                    newTraceGroup.Steps.Add(new UserTrace(userTrace.InstanceId, userTrace.ActivityParent, userTrace.Message));
                    continue;
                }

                if (step is BookmarkResumptionTrace bookmarkResumptionTrace)
                {
                    newTraceGroup.Steps.Add(new BookmarkResumptionTrace(bookmarkResumptionTrace.BookmarkName,
                        bookmarkResumptionTrace.SubinstanceId, bookmarkResumptionTrace.ActivityName));
                    continue;
                }

                if (step is SynchronizeTrace synchronizeTrace)
                {
                    newTraceGroup.Steps.Add(new SynchronizeTrace(synchronizeTrace.userTrace.InstanceId,
                        synchronizeTrace.userTrace.Message));
                    continue;
                }

                if (step is ActivityPlaceholderTrace activityPlaceholderTrace)
                {
                    newTraceGroup.Steps.Add(activityPlaceholderTrace);
                    continue;
                }

                if (step is TraceGroup tempTraceGroup)
                {
                    newTraceGroup.Steps.Add(TraceGroup.GetNewTraceGroup(tempTraceGroup));
                    continue;
                }
            }
            return newTraceGroup;
        }

        public override bool Equals(object obj)
        {
            if (obj is TraceGroup traceGroup)
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
