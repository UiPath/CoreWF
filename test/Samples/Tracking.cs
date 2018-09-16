// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf;
using CoreWf.Statements;
using CoreWf.Tracking;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Samples
{
    public class Tracking : IDisposable
    {
        private const string bookmarkName = "TrackingBookmark";
        private const string bookmarkData = "Bookmark Data";
        private Activity CreateWorkflow(string bookmarkName)
        {
            return new Sequence
            {
                DisplayName = "Tracking Sequence",
                Activities =
                {
                    new WriteLine { DisplayName = "Tracking WriteLine1", Text = "Tracking WriteLine1" },
                    new BookmarkActivity
                    {
                        DisplayName = "Tracking BookmarkActivity",
                        BookmarkName = bookmarkName,
                        Options = BookmarkOptions.None
                    },
                    new CustomTrackingActivity
                    {
                        DisplayName = "Tracking CustomTrackingActivity"
                    },
                    new WriteLine { DisplayName = "Tracking WriteLine2", Text = "Tracking WriteLine2" },
                }
            };
        }

        private void RunWorkflow(TrackingProfile profile, out MyTrackingParticipant participant)
        {
            AutoResetEvent completedEvent = new AutoResetEvent(false);
            AutoResetEvent idleEvent = new AutoResetEvent(false);
            Activity workflow = CreateWorkflow(bookmarkName);

            participant = new MyTrackingParticipant
            {
                TrackingProfile = profile
            };

            WorkflowApplication wfApp = Utilities.CreateWorkflowApplication(workflow,
                /*store=*/ null,
                idleEvent,
                /*unloadedEvent=*/ null,
                completedEvent);
            wfApp.Extensions.Add(participant);
            wfApp.Run();
            idleEvent.WaitOne(TimeSpan.FromSeconds(2));
            wfApp.ResumeBookmark(bookmarkName, bookmarkData);
            completedEvent.WaitOne(TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void InstanceTracking()
        {
            Console.WriteLine();
            Console.WriteLine("*** InstanceTracking ***");
            Console.WriteLine();
            MyTrackingParticipant participant;

            // First, let's just get all instance states tracked.
            TrackingProfile profile = new TrackingProfile();
            WorkflowInstanceQuery query = new WorkflowInstanceQuery();
            query.States.Add("*");
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.InstanceStates.Contains("Started"));
            Assert.True(participant.InstanceStates.Contains("Idle"));
            Assert.True(participant.InstanceStates.Contains("Completed"));

            // Now lets test filtering. Only ask for Idle and Unloaded.
            // We should only get Idle.

            profile = new TrackingProfile();
            query = new WorkflowInstanceQuery();
            query.States.Add("Idle");
            query.States.Add("Unloaded");
            profile.Queries.Add(query);

            RunWorkflow(profile, out participant);
            Assert.True(!participant.InstanceStates.Contains("Started"));
            Assert.True(participant.InstanceStates.Contains("Idle"));
            Assert.True(!participant.InstanceStates.Contains("Completed"));
            // Even though we asked for Unloaded records, there shouldn't be any.
            Assert.True(!participant.InstanceStates.Contains("Unloaded"));
        }
        [Fact]

        private void ActivityTracking()
        {
            Console.WriteLine();
            Console.WriteLine("*** ActivityTracking ***");
            Console.WriteLine();
            MyTrackingParticipant participant;

            // First, let's just get all activity states tracked.
            TrackingProfile profile = new TrackingProfile();
            ActivityStateQuery query = new ActivityStateQuery();
            query.States.Add("*");
            profile.Queries.Add(query);

            RunWorkflow(profile, out participant);
            Assert.True(participant.ActivityStates.Contains("Tracking WriteLine1:Executing"));
            Assert.True(participant.ActivityStates.Contains("Tracking WriteLine1:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Faulted"));

            Assert.True(participant.ActivityStates.Contains("Tracking BookmarkActivity:Executing"));
            Assert.True(participant.ActivityStates.Contains("Tracking BookmarkActivity:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking BookmarkActivity:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking BookmarkActivity:Faulted"));

            Assert.True(participant.ActivityStates.Contains("Tracking WriteLine2:Executing"));
            Assert.True(participant.ActivityStates.Contains("Tracking WriteLine2:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Faulted"));

            // Now lets test filtering by Activity name.

            profile = new TrackingProfile();
            query = new ActivityStateQuery();
            query.ActivityName = "Tracking BookmarkActivity";
            query.States.Add("*");
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Executing"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Faulted"));

            Assert.True(participant.ActivityStates.Contains("Tracking BookmarkActivity:Executing"));
            Assert.True(participant.ActivityStates.Contains("Tracking BookmarkActivity:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking BookmarkActivity:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking BookmarkActivity:Faulted"));

            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Executing"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Faulted"));

            // Now lets test filtering by Activity name AND specific states.

            profile = new TrackingProfile();
            query = new ActivityStateQuery();
            query.ActivityName = "Tracking BookmarkActivity";
            query.States.Add("Closed");
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Executing"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine1:Faulted"));

            Assert.True(!participant.ActivityStates.Contains("Tracking BookmarkActivity:Executing"));
            Assert.True(participant.ActivityStates.Contains("Tracking BookmarkActivity:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking BookmarkActivity:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking BookmarkActivity:Faulted"));

            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Executing"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Closed"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Canceled"));
            Assert.True(!participant.ActivityStates.Contains("Tracking WriteLine2:Faulted"));
        }
        [Fact]

        private void CustomTracking()
        {
            Console.WriteLine();
            Console.WriteLine("*** CustomTracking ***");
            Console.WriteLine();
            MyTrackingParticipant participant;

            // First, let's just get all activities and all custom records tracked.
            TrackingProfile profile = new TrackingProfile();
            CustomTrackingQuery query = new CustomTrackingQuery();
            query.ActivityName = "*";
            query.Name = "*";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.CustomRecords.Contains("CustomTrackKey:CustomTrackValue"));

            // Now lets test filtering by ActivityName.

            profile = new TrackingProfile();
            query = new CustomTrackingQuery();
            query.ActivityName = "Tracking CustomTrackingActivity";
            query.Name = "*";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.CustomRecords.Contains("CustomTrackKey:CustomTrackValue"));

            // Now lets test filtering by ActivityName with a name that is not there.

            profile = new TrackingProfile();
            query = new CustomTrackingQuery();
            query.ActivityName = "Tracking CustomTrackingActivity - Should not be there";
            query.Name = "*";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(!participant.CustomRecords.Contains("CustomTrackKey:CustomTrackValue"));

            // Now lets test filtering by CustomTrackingRecord.Name.

            profile = new TrackingProfile();
            query = new CustomTrackingQuery();
            query.ActivityName = "*";
            query.Name = "MyCustomTrackingRecord";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.CustomRecords.Contains("CustomTrackKey:CustomTrackValue"));

            // Now lets test filtering by CustomTrackingRecord.Name with a name that we don't expect.

            profile = new TrackingProfile();
            query = new CustomTrackingQuery();
            query.ActivityName = "*";
            query.Name = "MyCustomTrackingRecord-Should not be there";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(!participant.CustomRecords.Contains("CustomTrackKey:CustomTrackValue"));
        }

        [Fact]
        public void BookmarkResumptionTracking()
        {
            Console.WriteLine();
            Console.WriteLine("*** BookmarkResumptionTracking ***");
            Console.WriteLine();
            MyTrackingParticipant participant;

            // First, let's just get all bookmarks tracked.
            TrackingProfile profile = new TrackingProfile();
            BookmarkResumptionQuery query = new BookmarkResumptionQuery();
            query.Name = "*";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.BookmarkResumptionRecords.Contains(bookmarkName + ":" + bookmarkData));

            // Now lets test filtering.

            profile = new TrackingProfile();
            query = new BookmarkResumptionQuery();
            query.Name = bookmarkName;
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.BookmarkResumptionRecords.Contains(bookmarkName + ":" + bookmarkData));

            profile = new TrackingProfile();
            query = new BookmarkResumptionQuery();
            query.Name = "some bogus bookmark name";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(!participant.BookmarkResumptionRecords.Contains(bookmarkName + ":" + bookmarkData));
        }

        [Fact]
        public void ActivityScheduledTracking()
        {
            Console.WriteLine();
            Console.WriteLine("*** ActivityScheduledTracking ***");
            Console.WriteLine();
            MyTrackingParticipant participant;

            // First, let's just get all parent and child activities tracked.
            TrackingProfile profile = new TrackingProfile();
            ActivityScheduledQuery query = new ActivityScheduledQuery();
            query.ActivityName = "*";
            query.ChildActivityName = "*";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.ActivityScheduledRecords.Contains("Tracking Sequence:Tracking BookmarkActivity"));

            // Now lets test filtering.

            profile = new TrackingProfile();
            query = new ActivityScheduledQuery();
            query.ActivityName = "Tracking Sequence";
            query.ChildActivityName = "*";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.ActivityScheduledRecords.Contains("Tracking Sequence:Tracking BookmarkActivity"));

            profile = new TrackingProfile();
            query = new ActivityScheduledQuery();
            query.ActivityName = "Tracking Sequence";
            query.ChildActivityName = "Tracking BookmarkActivity";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(participant.ActivityScheduledRecords.Contains("Tracking Sequence:Tracking BookmarkActivity"));

            profile = new TrackingProfile();
            query = new ActivityScheduledQuery();
            query.ActivityName = "Incorrect Parent Activity Name";
            query.ChildActivityName = "Tracking BookmarkActivity";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(!participant.ActivityScheduledRecords.Contains("Tracking Sequence:Tracking BookmarkActivity"));

            profile = new TrackingProfile();
            query = new ActivityScheduledQuery();
            query.ActivityName = "Tracking Sequence";
            query.ChildActivityName = "Incorrect Child Activity Name";
            profile.Queries.Add(query);
            RunWorkflow(profile, out participant);
            Assert.True(!participant.ActivityScheduledRecords.Contains("Tracking Sequence:Tracking BookmarkActivity"));
        }

        public void Dispose()
        {
        }
    }

    internal class MyTrackingParticipant : TrackingParticipant
    {
        private List<string> _instanceStates;
        private List<string> _activityStates;
        private List<string> _customRecords;
        private List<string> _bookmarkResumptionRecords;
        private List<string> _activityScheduledRecords;

        private object _lockObject = new object();

        public List<string> InstanceStates
        {
            get
            {
                if (_instanceStates == null)
                {
                    lock (_lockObject)
                    {
                        _instanceStates = new List<string>();
                    }
                }
                return _instanceStates;
            }
        }

        public List<string> ActivityStates
        {
            get
            {
                if (_activityStates == null)
                {
                    lock (_lockObject)
                    {
                        _activityStates = new List<string>();
                    }
                }
                return _activityStates;
            }
        }

        public List<string> CustomRecords
        {
            get
            {
                if (_customRecords == null)
                {
                    lock (_lockObject)
                    {
                        _customRecords = new List<string>();
                    }
                }
                return _customRecords;
            }
        }

        public List<string> BookmarkResumptionRecords
        {
            get
            {
                if (_bookmarkResumptionRecords == null)
                {
                    lock (_lockObject)
                    {
                        _bookmarkResumptionRecords = new List<string>();
                    }
                }
                return _bookmarkResumptionRecords;
            }
        }

        public List<string> ActivityScheduledRecords
        {
            get
            {
                if (_activityScheduledRecords == null)
                {
                    lock (_lockObject)
                    {
                        _activityScheduledRecords = new List<string>();
                    }
                }
                return _activityScheduledRecords;
            }
        }

        protected override void Track(TrackingRecord record, TimeSpan timeout)
        {
            WorkflowInstanceRecord instanceRecord = record as WorkflowInstanceRecord;
            if (instanceRecord != null)
            {
                this.InstanceStates.Add(instanceRecord.State);
                return;
            }

            ActivityStateRecord activityStateRecord = record as ActivityStateRecord;
            if (activityStateRecord != null)
            {
                this.ActivityStates.Add(Colonize(activityStateRecord.Activity.Name, activityStateRecord.State));
                return;
            }

            CustomTrackingRecord customRecord = record as CustomTrackingRecord;
            if (customRecord != null)
            {
                foreach (KeyValuePair<string, object> kvp in customRecord.Data)
                {
                    this.CustomRecords.Add(Colonize(kvp.Key, kvp.Value.ToString()));
                }
                return;
            }

            BookmarkResumptionRecord bookmarkRecord = record as BookmarkResumptionRecord;
            if (bookmarkRecord != null)
            {
                this.BookmarkResumptionRecords.Add(Colonize(bookmarkRecord.BookmarkName, bookmarkRecord.Payload.ToString()));
                return;
            }

            ActivityScheduledRecord scheduledRecord = record as ActivityScheduledRecord;
            if (scheduledRecord != null)
            {
                // For the root activity, the "parent" will be null, so tolerate that case.
                this.ActivityScheduledRecords.Add(Colonize((scheduledRecord.Activity != null) ? scheduledRecord.Activity.Name : "<null>", scheduledRecord.Child.Name));
                return;
            }
        }
        private string Colonize(string one, string two)
        {
            return one + ":" + two;
        }
    }

    internal class CustomTrackingActivity : NativeActivity
    {
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
        }

        protected override void Execute(NativeActivityContext context)
        {
            CustomTrackingRecord record = new CustomTrackingRecord("MyCustomTrackingRecord");
            record.Data.Add("CustomTrackKey", "CustomTrackValue");
            context.Track(record);
        }
    }
}
