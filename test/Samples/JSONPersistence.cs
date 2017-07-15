// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using JsonFileInstanceStore;
using CoreWf;
using CoreWf.Statements;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Samples
{
    public class JSONPersistence : IDisposable
    {
        private AutoResetEvent _completedEvent = new AutoResetEvent(false);
        private AutoResetEvent _unloadedEvent = new AutoResetEvent(false);
        private WorkflowApplication _wfApp;
        private const string bookmarkName = "JSONPersistenceBookmarkName";

        [Fact]
        public void RunTest()
        {
            Sequence workflow = new Sequence
            {
                Activities =
                {
                    new BookmarkActivity
                    {
                        BookmarkName = new InArgument<string>(bookmarkName),
                        Options = BookmarkOptions.MultipleResume
                    }
                }
            };

            FileInstanceStore store = new FileInstanceStore(Directory.GetCurrentDirectory());

            _wfApp = Utilities.CreateWorkflowApplication(workflow, store, null, _unloadedEvent, _completedEvent, null);
            _wfApp.Run();
            Guid instanceId = _wfApp.Id;
            _unloadedEvent.WaitOne(TimeSpan.FromSeconds(2));

            // Need to create a new instance of WorkflowApplication every time we load the instance.
            _wfApp = Utilities.CreateWorkflowApplication(workflow, store, null, _unloadedEvent, _completedEvent, null);
            _wfApp.Load(instanceId);
            _wfApp.ResumeBookmark(bookmarkName, "one");
            _unloadedEvent.WaitOne(TimeSpan.FromSeconds(2));

            _wfApp = Utilities.CreateWorkflowApplication(workflow, store, null, _unloadedEvent, _completedEvent, null);
            _wfApp.Load(instanceId);
            _wfApp.ResumeBookmark(bookmarkName, "two");
            _unloadedEvent.WaitOne(TimeSpan.FromSeconds(2));

            _wfApp = Utilities.CreateWorkflowApplication(workflow, store, null, _unloadedEvent, _completedEvent, null);
            _wfApp.Load(instanceId);
            _wfApp.ResumeBookmark(bookmarkName, "three");
            _unloadedEvent.WaitOne(TimeSpan.FromSeconds(2));

            _wfApp = Utilities.CreateWorkflowApplication(workflow, store, null, _unloadedEvent, _completedEvent, null);
            _wfApp.Load(instanceId);
            _wfApp.ResumeBookmark(bookmarkName, "stop");
            _completedEvent.WaitOne(TimeSpan.FromSeconds(2));
        }

        public void Dispose()
        {
        }
    }
}
