// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;

namespace Test.Common.TestObjects.Utilities.Validation
{
    /// <summary>
    /// Root class of the expected trace data structure
    /// </summary>
    ///
    [DataContract]
    public class ExpectedTrace
    {
        public TraceGroup Trace = null;

        /// <summary>
        /// Verify than no other steps occured, except for listed ones
        /// </summary>
        internal bool verifyCompleteness = true;

        /// <summary>
        /// This flag can be used to change the verification of the tracing.
        /// If true, the activity traces will get sorted and seperated from all other traces and 
        /// sorted into a tree, based on the hierarchy of activities.
        /// </summary>
        public bool SortBeforeVerification = false;

        /// <summary>
        /// Depending on the value of userVerifyTypes, either verify only types on the verify list
        /// or verify only types not in the ignore list
        /// </summary>
        private HashSet<String> _verifyTypes = new HashSet<String>();
        private HashSet<String> _ignoreTypes = new HashSet<String>();
        private HashSet<WorkflowInstanceState> _ignoredStates = new HashSet<WorkflowInstanceState>();
        private List<TraceFilter> _filters;


        private bool _useVerifyTypes = false;

        public ExpectedTrace()
        {
            AddIgnoredState(WorkflowInstanceState.Persisted);
            AddIgnoredState(WorkflowInstanceState.Idle);
            AddIgnoredState(WorkflowInstanceState.Suspended);
            AddIgnoredState(WorkflowInstanceState.Unsuspended);
            AddIgnoredState(WorkflowInstanceState.Deleted);

            this.Filters.Add(new InternalActivityFilter());
        }

        public ExpectedTrace(TraceGroup trace)
            : this()
        {
            this.Trace = trace;
        }

        public ExpectedTrace(ExpectedTrace expectedTrace)
        {
            this.Trace = TraceGroup.GetNewTraceGroup(expectedTrace.Trace);
            this.SortBeforeVerification = expectedTrace.SortBeforeVerification;
            this.Filters.AddRange(expectedTrace.Filters);
            CopyVerifyAndIgnoreTypes(expectedTrace);
        }

        // Collection of filters to apply to the traces
        public List<TraceFilter> Filters
        {
            get
            {
                if (_filters == null)
                {
                    _filters = new List<TraceFilter>();
                }
                return _filters;
            }
        }

        public void ClearIgnoredStates()
        {
            _ignoredStates.Clear();
        }

        public void AddIgnoredState(WorkflowInstanceState state)
        {
            _ignoredStates.Add(state);
        }

        public void AddIgnoreTypes(params Type[] types)
        {
            AddIgnoreTypes(true, types);
        }

        internal void AddIgnoreTypes(bool updateVerifyType, params Type[] types)
        {
            if (updateVerifyType)
            {
                _useVerifyTypes = false;
            }

            if (types == null)
            {
                return;
            }

            foreach (Type ignoreType in types)
            {
                _ignoreTypes.Add(ignoreType.FullName);
            }
        }

        public void AddVerifyTypes(params Type[] types)
        {
            _useVerifyTypes = true;

            if (types == null)
            {
                return;
            }

            foreach (Type verifyType in types)
            {
                _verifyTypes.Add(verifyType.FullName);
            }
        }

        public void CopyVerifyAndIgnoreTypes(ExpectedTrace expectedTrace)
        {
            _useVerifyTypes = expectedTrace._useVerifyTypes;
            _ignoreTypes = new HashSet<String>(expectedTrace._ignoreTypes);
            _verifyTypes = new HashSet<String>(expectedTrace._verifyTypes);
            _ignoredStates = new HashSet<WorkflowInstanceState>(expectedTrace._ignoredStates);
        }

        public override string ToString()
        {
            using (StringWriter stringWriter = new StringWriter())
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = true
                };

                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                {
                    this.Trace.WriteXml(xmlWriter);
                }
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Depending on the value of userVerifyTypes, either verify only types on the verify list
        /// or verify only types not in the ignore list
        /// </summary>
        /// <param name="step">Step to check</param>
        internal bool CanBeIgnored(IActualTraceStep step)
        {
            // If this is a workflow instance trace, and the instance is in the ignored collection, ignore it
            if (step is WorkflowInstanceTrace && _ignoredStates.Contains(((WorkflowInstanceTrace)step).InstanceStatus))
            {
                return true;
            }

            if (_useVerifyTypes)
            {
                return !_verifyTypes.Contains(step.GetType().FullName);
            }
            else
            {
                return _ignoreTypes.Contains(step.GetType().FullName);
            }
        }
    }
}

