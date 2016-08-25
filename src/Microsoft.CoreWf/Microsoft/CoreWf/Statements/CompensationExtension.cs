// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Hosting;
using Microsoft.CoreWf.Persistence;
using Microsoft.CoreWf.Runtime;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.CoreWf.Statements
{
    public class CompensationExtension : PersistenceParticipant, IWorkflowInstanceExtension
    {
        private static readonly XNamespace s_compensationNamespace = XNamespace.Get("urn:schemas-microsoft-com:Microsoft.CoreWf/4.0/compensation");
        private static readonly XName s_compensationExtensionData = s_compensationNamespace.GetName("Data");
        [Fx.Tag.SynchronizationObject(Blocking = false)]

        private Dictionary<long, CompensationTokenData> _compensationTokenTable;

        public CompensationExtension()
        {
            _compensationTokenTable = new Dictionary<long, CompensationTokenData>();
        }

        internal Dictionary<long, CompensationTokenData> CompensationTokenTable
        {
            get
            {
                return _compensationTokenTable;
            }
            private set
            {
                _compensationTokenTable = value;
            }
        }

        internal long Id
        {
            get;
            set;
        }

        internal Bookmark WorkflowCompensation
        {
            get;
            set;
        }

        internal Bookmark WorkflowConfirmation
        {
            get;
            set;
        }

        internal Bookmark WorkflowCompensationScheduled
        {
            get;
            private set;
        }

        internal bool IsWorkflowCompensationBehaviorScheduled
        {
            get;
            private set;
        }

        internal WorkflowInstanceProxy Instance
        {
            get;
            private set;
        }

        internal void Add(long compensationId, CompensationTokenData compensationToken)
        {
            Fx.Assert(compensationToken != null, "compensationToken must be valid");

            this.CompensationTokenTable[compensationId] = compensationToken;
        }

        internal void Remove(long compensationId)
        {
            this.CompensationTokenTable.Remove(compensationId);
        }

        internal CompensationTokenData Get(long compensationId)
        {
            CompensationTokenData compensationToken = null;
            this.CompensationTokenTable.TryGetValue(compensationId, out compensationToken);
            return compensationToken;
        }

        internal Bookmark FindBookmark(long compensationId, CompensationBookmarkName bookmarkName)
        {
            CompensationTokenData compensationToken = null;
            Bookmark bookmark = null;

            if (this.CompensationTokenTable.TryGetValue(compensationId, out compensationToken))
            {
                bookmark = compensationToken.BookmarkTable[bookmarkName];
            }

            return bookmark;
        }

        internal void SetupWorkflowCompensationBehavior(NativeActivityContext context, BookmarkCallback callback, Activity workflowCompensationBehavior)
        {
            this.WorkflowCompensationScheduled = context.CreateBookmark(callback);

            Fx.Assert(workflowCompensationBehavior != null, "WorkflowCompensationBehavior must be valid");
            context.ScheduleSecondaryRoot(workflowCompensationBehavior, null);

            // Add the root compensationToken to track all root CA execution order.
            this.Add(CompensationToken.RootCompensationId, new CompensationTokenData(CompensationToken.RootCompensationId, CompensationToken.RootCompensationId));
            this.IsWorkflowCompensationBehaviorScheduled = true;
        }

        internal long GetNextId()
        {
            return ++this.Id;
        }

        internal void NotifyMessage(NativeActivityContext context, long compensationId, CompensationBookmarkName compensationBookmark)
        {
            Bookmark bookmark = FindBookmark(compensationId, compensationBookmark);

            if (bookmark != null)
            {
                context.ResumeBookmark(bookmark, compensationId);
            }
            else
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.BookmarkNotRegistered(compensationBookmark)));
            }
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.InterfaceMethodsShouldBeCallableByChildTypes,
        //Justification = "The inherit class don't need to call this method or access this method")]
        IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
        {
            return null;
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.InterfaceMethodsShouldBeCallableByChildTypes,
        //Justification = "The inherit class don't need to call this method or access this method")]
        void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
        {
            this.Instance = instance;
        }

        // PersistenceParticipant
        protected override void CollectValues(out IDictionary<XName, object> readWriteValues, out IDictionary<XName, object> writeOnlyValues)
        {
            writeOnlyValues = null;
            readWriteValues = new Dictionary<XName, object>(1)
            {
                {
                    s_compensationExtensionData,
                    new List<object>(6)
                    {
                        this.CompensationTokenTable,
                        this.WorkflowCompensation,
                        this.WorkflowConfirmation,
                        this.WorkflowCompensationScheduled,
                        this.IsWorkflowCompensationBehaviorScheduled,
                        this.Id
                    }
                }
            };
        }

        protected override void PublishValues(IDictionary<XName, object> readWriteValues)
        {
            object data;

            if (readWriteValues.TryGetValue(s_compensationExtensionData, out data))
            {
                List<object> list = (List<object>)data;
                this.CompensationTokenTable = (Dictionary<long, CompensationTokenData>)list[0];
                this.WorkflowCompensation = (Bookmark)list[1];
                this.WorkflowConfirmation = (Bookmark)list[2];
                this.WorkflowCompensationScheduled = (Bookmark)list[3];
                this.IsWorkflowCompensationBehaviorScheduled = (bool)list[4];
                this.Id = (long)list[5];
            }
        }
    }
}
