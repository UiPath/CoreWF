// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Test.Common.TestObjects.Utilities.Validation
{
    using System;
    using Microsoft.CoreWf.Tracking;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml;

#if !SILVERLIGHT
    [DataContract]
#endif

    public class ActualTrace
    {
        private List<IActualTraceStep> _steps = new List<IActualTraceStep>();

        public ActualTrace()
        {
        }

        public ActualTrace(ActualTrace actualTrace)
        {
            lock (actualTrace.Steps)
            {
                foreach (IActualTraceStep step in actualTrace.Steps)
                {
                    ActivityTrace activityTrace = step as ActivityTrace;
                    if (activityTrace != null)
                    {
                        this.Steps.Add(new ActivityTrace(activityTrace));
                        continue;
                    }

                    //WorkflowInstanceUpdatedTrace workflowInstanceUpdatedTrace = step as WorkflowInstanceUpdatedTrace;
                    //if (workflowInstanceUpdatedTrace != null)
                    //{
                    //    this.Steps.Add(new WorkflowInstanceUpdatedTrace(workflowInstanceUpdatedTrace.InstanceName, workflowInstanceUpdatedTrace.OriginalWorkflowIdentity, workflowInstanceUpdatedTrace.WorkflowDefinitionIdentity, workflowInstanceUpdatedTrace.InstanceStatus));
                    //    continue;
                    //}

                    WorkflowInstanceTrace workflowInstanceTrace = step as WorkflowInstanceTrace;
                    if (workflowInstanceTrace != null)
                    {
                        this.Steps.Add(new WorkflowInstanceTrace(workflowInstanceTrace.InstanceName, workflowInstanceTrace.WorkflowDefinitionIdentity, workflowInstanceTrace.InstanceStatus));
                        continue;
                    }

                    UserTrace userTrace = step as UserTrace;
                    if (userTrace != null)
                    {
                        this.Steps.Add(new UserTrace(userTrace.InstanceId, userTrace.ActivityParent, userTrace.Message));
                        continue;
                    }

                    BookmarkResumptionTrace bookmarkResumptionTrace = step as BookmarkResumptionTrace;
                    if (bookmarkResumptionTrace != null)
                    {
                        this.Steps.Add(new BookmarkResumptionTrace(bookmarkResumptionTrace.BookmarkName, bookmarkResumptionTrace.SubinstanceId,
                            bookmarkResumptionTrace.ActivityName));
                        continue;
                    }

                    SynchronizeTrace synchronizeTrace = step as SynchronizeTrace;
                    if (synchronizeTrace != null)
                    {
                        this.Steps.Add(new SynchronizeTrace(synchronizeTrace.userTrace.InstanceId,
                            synchronizeTrace.userTrace.Message));
                        continue;
                    }

                    WorkflowExceptionTrace weTrace = step as WorkflowExceptionTrace;
                    if (weTrace != null)
                    {
                        this.Steps.Add(new WorkflowExceptionTrace(weTrace.InstanceName, weTrace.InstanceException));
                        continue;
                    }

                    WorkflowAbortedTrace wasTrace = step as WorkflowAbortedTrace;
                    if (wasTrace != null)
                    {
                        this.Steps.Add(new WorkflowAbortedTrace(wasTrace.InstanceId, wasTrace.AbortedReason));
                        continue;
                    }
                }
            }
        }

        public List<IActualTraceStep> Steps
        {
            get { return _steps; }
        }

        public void Add(IActualTraceStep step)
        {
            lock (_steps)
            {
                if (step.TimeStamp == default(DateTime))
                {
                    step.TimeStamp = DateTime.Now;
                }

                _steps.Add(step);
            }
        }

        /// <summary>
        /// This method removes all of the non-acitivity traces, and then orders the activity traces so they will match
        ///  more closely the expected traces. This is required for complex parallel traces.
        /// </summary>
        public void OrderTraces()
        {
            List<ActivityTrace> traceSteps = new List<ActivityTrace>();
            List<UserTrace> userTraces = new List<UserTrace>();

            // Find the starting node
            lock (this.Steps)
            {
                foreach (IActualTraceStep ts in this.Steps)
                {
                    if (ts is ActivityTrace)
                    {
                        traceSteps.Add(ts as ActivityTrace);
                    }
                    else if (ts is UserTrace)
                    {
                        userTraces.Add(ts as UserTrace);
                    }
                    else
                    {
                        //Log.TraceInternal("[ActualTrace]ParallelValidation removing trace " + ts.ToString());
                    }
                }
            }

            Dictionary<string, ATNode> sortedSteps = new Dictionary<string, ATNode>();
            ATNode rootNode = null;
            this.Steps.Clear();

            // do this until we have completely constructed the tree, in case traces get written out in 
            //  an order we arent expecting, loop until either we cant make any more changes, or until 
            //  all the traces have been ordered.
            bool changed;
            while (traceSteps.Count > 0)
            {
                changed = false;
                for (int x = 0; x < traceSteps.Count; x++)
                {
                    ActivityTrace at = traceSteps[x];
                    ATNode parentnode;

                    // If a new activity was scheduled
                    if (at.IsScheduled)
                    {
                        // When activityID is null, this is the root activity.
                        if (at.ActivityId == null)
                        {
                            rootNode = new ATNode()
                            {
                                trace = at,
                            };

                            // add root node to the tree using its' activity id and instanceid (for when activities get 
                            //  instantiated multiple times, like with parallelforeach
                            sortedSteps[1 + ":" + 1] = rootNode;
                            traceSteps.RemoveAt(x--);
                            changed = true;
                        }
                        // otherwise, if parent has already been found, add this node
                        else if (sortedSteps.TryGetValue(at.ActivityId + ":" + at.ActivityInstanceId, out parentnode))
                        {
                            ATNode newnode = new ATNode()
                            {
                                trace = at,
                                parent = parentnode
                            };
                            // add the node to its parent.
                            sortedSteps[at.ChildActivityId + ":" + at.ChildActivityInstanceId] = newnode;
                            parentnode.children.Add(newnode);
                            traceSteps.RemoveAt(x--);
                            changed = true;
                        }
                    }
                    else
                    {
                        if (sortedSteps.TryGetValue(at.ActivityId + ":" + at.ActivityInstanceId, out parentnode))
                        {
                            ATNode node = new ATNode()
                            {
                                trace = at,
                                parent = parentnode.parent
                            };

                            // if root node, just add as the last child, if it ends up being out of order the 
                            //  validation logic will correct it.
                            if (parentnode.parent == null)
                            {
                                parentnode.children.Add(node);
                            }
                            else
                            {
                                // otherwise just setup parent
                                parentnode.parent.children.Add(node);
                            }
                            traceSteps.RemoveAt(x--);
                            changed = true;
                        }
                    }
                }

                if (rootNode == null)
                {
                    // there is no root node, we cant construct without it
                    //Log.TraceInternal("[ActualTrace]Parallel tracing couldnt find a root node, using the unordered traces.");
                    return;
                }

                if (!changed)
                {
                    // the tree is in a state that we cant reconstruct
                    //Log.TraceInternal("[ActualTrace]Parallel tracing couldnt find the parent for a node, using the unordered traces");
                    return;
                }
            }

            foreach (UserTrace ut in userTraces)
            {
                (sortedSteps[ut.ActivityParent]).userTraces.Add(ut);
            }

            lock (this.Steps)
            {
                this.Steps.AddRange(rootNode.GetTraces());
            }
        }

        public void Validate(ExpectedTrace expectedTrace)
        {
            this.Validate(expectedTrace, true);
        }

        public void Validate(ExpectedTrace expectedTrace, bool logTraces)
        {
            lock (_steps)
            {
                if (expectedTrace.SortBeforeVerification)
                {
                    // copy the expected trace, remove activity traces and verify workflow instnace traces
                    ExpectedTrace etrace = new ExpectedTrace(expectedTrace);
                    ActualTrace atrace = new ActualTrace(this);
                    etrace.AddIgnoreTypes(typeof(ActivityTrace), typeof(UserTrace));
                    TraceValidator.Validate(atrace, etrace, logTraces);

                    // now verify the activity traces, after they have been ordered
                    expectedTrace.AddIgnoreTypes(typeof(WorkflowInstanceTrace));
                    expectedTrace.AddIgnoreTypes(typeof(UserTrace));
                    this.OrderTraces();
                    TraceValidator.Validate(this, expectedTrace);
                }
                else
                {
                    TraceValidator.Validate(this, expectedTrace, logTraces);
                }
            }
        }

        public override string ToString()
        {
            using (StringWriter stringWriter = new StringWriter())
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings();
                xmlSettings.Indent = true;
                xmlSettings.OmitXmlDeclaration = true;
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                {
                    OrderedTraces orderedTraces = new OrderedTraces();

                    lock (_steps)
                    {
                        foreach (IActualTraceStep step in _steps)
                        {
                            if (step is WorkflowTraceStep)
                            {
                                orderedTraces.Steps.Add(step as WorkflowTraceStep);
                            }
                        }
                    }

                    orderedTraces.WriteXml(xmlWriter);
                }
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Helper class used to create the Trace hierarchy
        /// </summary>
        private class ATNode
        {
            public ActivityTrace trace;
            public ATNode parent;
            public List<ATNode> children = new List<ATNode>();
            public List<UserTrace> userTraces = new List<UserTrace>();

            public List<IActualTraceStep> GetTraces()
            {
                if (children.Count > 0 && userTraces.Count > 0)
                {
                    throw new ArgumentException("Cant have a user trace and a activity trace in an activity");
                }

                List<IActualTraceStep> nodes = new List<IActualTraceStep>();
                nodes.Add(trace);

                foreach (UserTrace ut in userTraces)
                {
                    nodes.Add(ut);
                }

                foreach (ATNode node in children)
                {
                    nodes.AddRange(node.GetTraces());
                }


                return nodes;
            }
        }
    }
}

