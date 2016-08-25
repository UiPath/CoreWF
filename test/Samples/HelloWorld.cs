// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Statements;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Samples
{
    public class HelloWorld
    {
        private readonly ITestOutputHelper _output;

        public HelloWorld(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void WriteToConsole()
        {
            var workflow1 = new Sequence() { DisplayName = "Hello World Sequence" };
            workflow1.Activities.Add(new WriteLine() { Text = "Hello World!", DisplayName = "Display greeting" });
            WorkflowInvoker.Invoke(workflow1);
        }

        [Fact(Skip = "ETW event ids have changed, this test needs to be fixed")]
        public void EtwEvents()
        {
            List<EventWrittenEventArgs> recordedEvents = null;
            using (WfTracingEventListener verboseListener = new WfTracingEventListener())
            {
                verboseListener.EnableEvents(WfEventSource.Instance, EventLevel.Verbose);

                var workflow1 = new Sequence() { DisplayName = "Hello World Sequence" };
                workflow1.Activities.Add(new WriteLine() { Text = "Hello World!", DisplayName = "Display greeting" });
                WorkflowInvoker.Invoke(workflow1);
                recordedEvents = verboseListener.RecordedEvents;
            }

            var sr = new StringReader(HelloWorldEtwEvents);
            sr.ReadLine();
            string line = null;
            List<CompareEvent> expectedEvents = new List<CompareEvent>();
            while ((line = sr.ReadLine()) != null)
                expectedEvents.Add(new CompareEvent(line));

            foreach (var actualEvent in recordedEvents)
            {
                bool isMatch = false;
                int i;
                for (i = 0; i < expectedEvents.Count; i++)
                {
                    var expectedEvent = expectedEvents[i];
                    if (expectedEvent.EventId != actualEvent.EventId)
                        continue;
                    if (expectedEvent.Level != actualEvent.Level)
                        continue;
                    bool keywordsMatch = true;
                    foreach (var keywordField in typeof(WfEventSource.Keywords).GetTypeInfo().DeclaredFields)
                    {
                        long keyword = (long)keywordField.GetValue(null);
                        if ((expectedEvent.Keywords & keyword) != ((long)actualEvent.Keywords & keyword))
                        {
                            keywordsMatch = false;
                            break;
                        }
                    }
                    if (!keywordsMatch)
                        continue;
                    var payload = actualEvent.Payload.ToArray();
                    Guid temp;
                    if (payload.Length >= 1 && (payload[0].GetType() == typeof(Guid) || (payload[0].GetType() == typeof(string) && Guid.TryParse(payload[0] as string, out temp))))
                        payload[0] = string.Empty;
                    var actualMessage = string.Format(actualEvent.Message, payload);
                    if (expectedEvent.Message != actualMessage)
                        continue;
                    isMatch = true;
                    break;
                }

                Assert.True(isMatch, string.Format("ID: {0}, Level: {1}, Keywords: {2}, Message: {3}", actualEvent.EventId, actualEvent.Level, actualEvent.Keywords, string.Format(actualEvent.Message, actualEvent.Payload.ToArray())));
                expectedEvents.RemoveAt(i);
            }

            Assert.Empty(expectedEvents);
        }

        private class CompareEvent
        {
            public int EventId { get; set; }
            public EventLevel Level { get; set; }
            public long Keywords { get; set; }
            public string Message { get; set; }

            public CompareEvent(string line)
            {
                var fields = line.Split('\t');
                EventId = int.Parse(fields[0]);
                Level = (EventLevel)int.Parse(fields[1]);
                Keywords = long.Parse(fields[2]);
                Message = fields[12];
            }

            public override string ToString()
            {
                return string.Format("ID: {0}, Level: {1}, Keywords: {2}, Message: {3}", EventId, Level, Keywords, Message);
            }
        }

        // This table was dumped from a .NET framework workflow trace using svcperf.
        // Some corrections were made to the messages since the .NET framework will sometimes incorrectly truncate the last character
        private const string HelloWorldEtwEvents = @"Id	Level	Keywords	Task	Opcode	Symbol	TimeStamp	Delta (ms)	Pid	Tid	ActivityId	RelatedActivityId	Message
2027	5	1152921504623624192	CacheRootMetadata	win:Start	CacheRootMetadataStart	0:21:26:44.162215	11234	18152	9636			CacheRootMetadata started on activity 'Hello World Sequence'
2024	5	1152921504623624192	InternalCacheMetadata	win:Start	InternalCacheMetadataStart	0:21:26:44.175177	13	18152	9636			InternalCacheMetadata started on activity '1'.
2025	5	1152921504623624192	InternalCacheMetadata	win:Stop	InternalCacheMetadataStop	0:21:26:44.179749	5	18152	9636			InternalCacheMetadata stopped on activity '1'.
2024	5	1152921504623624192	InternalCacheMetadata	win:Start	InternalCacheMetadataStart	0:21:26:44.195440	16	18152	9636			InternalCacheMetadata started on activity '2'.
2025	5	1152921504623624192	InternalCacheMetadata	win:Stop	InternalCacheMetadataStop	0:21:26:44.214432	19	18152	9636			InternalCacheMetadata stopped on activity '2'.
2024	5	1152921504623624192	InternalCacheMetadata	win:Start	InternalCacheMetadataStart	0:21:26:44.222141	8	18152	9636			InternalCacheMetadata started on activity '3'.
2025	5	1152921504623624192	InternalCacheMetadata	win:Stop	InternalCacheMetadataStop	0:21:26:44.226348	4	18152	9636			InternalCacheMetadata stopped on activity '3'.
2028	5	1152921504623624192	CacheRootMetadata	win:Stop	CacheRootMetadataStop	0:21:26:44.227547	1	18152	9636			CacheRootMetadata stopped on activity Hello World Sequence.
1009	4	1152921504623624192	ScheduleActivity	win:Info	ActivityScheduled	0:21:26:44.239023	11	18152	9636			Parent Activity '', DisplayName: '', InstanceId: '' scheduled child Activity 'Microsoft.CoreWf.Statements.Sequence', DisplayName: 'Hello World Sequence', InstanceId: '1'.
2021	5	1152921504623624192	ExecuteWorkItem	win:Start	ExecuteWorkItemStart	0:21:26:44.248136	9	18152	9636			Execute work item start
1009	4	1152921504623624192	ScheduleActivity	win:Info	ActivityScheduled	0:21:26:44.294120	46	18152	9636			Parent Activity 'Microsoft.CoreWf.Statements.Sequence', DisplayName: 'Hello World Sequence', InstanceId: '1' scheduled child Activity 'Microsoft.CoreWf.Statements.WriteLine', DisplayName: 'Display greeting', InstanceId: '2'.
2022	5	1152921504623624192	ExecuteWorkItem	win:Stop	ExecuteWorkItemStop	0:21:26:44.299800	6	18152	9636			Execute work item stop
2021	5	1152921504623624192	ExecuteWorkItem	win:Start	ExecuteWorkItemStart	0:21:26:44.299803	0	18152	9636			Execute work item start
1040	5	1152921504640401408	ExecuteActivity	win:Info	InArgumentBound	0:21:26:44.305561	0	18152	9636			In argument 'TextWriter' on Activity 'Microsoft.CoreWf.Statements.WriteLine', DisplayName: 'Display greeting', InstanceId: '2' has been bound with value: <Null>.
1040	5	1152921504640401408	ExecuteActivity	win:Info	InArgumentBound	0:21:26:44.305556	6	18152	9636			In argument 'Text' on Activity 'Microsoft.CoreWf.Statements.WriteLine', DisplayName: 'Display greeting', InstanceId: '2' has been bound with value: 'Hello World!'.
1010	4	1152921504623624192	CompleteActivity	win:Info	ActivityCompleted	0:21:26:44.321114	16	18152	9636			Activity 'Microsoft.CoreWf.Statements.WriteLine', DisplayName: 'Display greeting', InstanceId: '2' has completed in the 'Closed' state.
2022	5	1152921504623624192	ExecuteWorkItem	win:Stop	ExecuteWorkItemStop	0:21:26:44.321488	0	18152	9636			Execute work item stop
2021	5	1152921504623624192	ExecuteWorkItem	win:Start	ExecuteWorkItemStart	0:21:26:44.321490	0	18152	9636			Execute work item start
1010	4	1152921504623624192	CompleteActivity	win:Info	ActivityCompleted	0:21:26:44.325847	4	18152	9636			Activity 'Microsoft.CoreWf.Statements.Sequence', DisplayName: 'Hello World Sequence', InstanceId: '1' has completed in the 'Closed' state.
2022	5	1152921504623624192	ExecuteWorkItem	win:Stop	ExecuteWorkItemStop	0:21:26:44.326194	0	18152	9636			Execute work item stop
1001	4	1152921504623624192	WFApplicationStateChange	Completed	WorkflowApplicationCompleted	0:21:26:44.340843	15	18152	9636			WorkflowInstance Id: '' has completed in the Closed state.
";
    }
}
