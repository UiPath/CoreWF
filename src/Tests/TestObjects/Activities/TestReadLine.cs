// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Activities;
using System.Threading;
using Test.Common.TestObjects.Utilities;
using Test.Common.TestObjects.Utilities.Validation;

namespace Test.Common.TestObjects.Activities
{
    public class ReadLine<T> : NativeActivity
    {
        private string _bookmarkName;

        public ReadLine()
        {
        }

        public ReadLine(string bookmarkName)
        {
            _bookmarkName = bookmarkName;
        }

        public OutArgument<T> BookmarkValue { get; set; }

        public string BookmarkName
        {
            get
            {
                return _bookmarkName;
            }
            set
            {
                _bookmarkName = value;
            }
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument bookmarkValueArgument = new RuntimeArgument("BookmarkValue", typeof(T), ArgumentDirection.Out);
            metadata.Bind(this.BookmarkValue, bookmarkValueArgument);

            metadata.AddArgument(bookmarkValueArgument);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (!String.IsNullOrEmpty(this.BookmarkName))
            {
                context.CreateBookmark(this.BookmarkName, new BookmarkCallback(OnResumeBookmark));
            }
        }

        protected virtual void OnResumeBookmark(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            if (!(obj is T))
            {
                throw new Exception("Resume Bookmark with object of type " + typeof(T).FullName);
            }

            BookmarkValue.Set(context, (T)obj);
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }

    public class TestReadLine<T> : TestActivity
    {
        protected ReadLine<T> productReadLine;

        public TestReadLine(string bookMarkName, string displayName)
        {
            this.productReadLine = new ReadLine<T>(bookMarkName);
            this.ProductActivity = this.productReadLine;
            this.DisplayName = displayName;
        }

        public TestReadLine() :
            this(String.Empty, Guid.NewGuid().ToString())
        {
        }

        public string BookmarkName
        {
            get
            {
                return this.productReadLine.BookmarkName;
            }
            set
            {
                this.productReadLine.BookmarkName = value;
            }
        }

        public Activity<Location<T>> BookmarkValue
        {
            get
            {
                return this.productReadLine.BookmarkValue.Expression;
            }
            set
            {
                this.productReadLine.BookmarkValue = new OutArgument<T>(value);
            }
        }
    }

    public class WaitReadLine<T> : NativeActivity
    {
        public const string BeforeWait = "Before Wait - Inside OnResumeBookmark";
        public const string AfterWait = "After Wait - Inside OnResumeBookmark";

        public WaitReadLine()
        {
        }

        public WaitReadLine(string bookmarkName)
        {
            this.BookmarkName = bookmarkName;
            this.WaitTime = TimeSpan.FromSeconds(10);
        }

        public OutArgument<T> BookmarkValue { get; set; }
        public string BookmarkName { get; set; }
        public TimeSpan WaitTime { get; set; }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            RuntimeArgument bookmarkValueArgument = new RuntimeArgument("BookmarkValue", typeof(T), ArgumentDirection.Out);
            metadata.Bind(this.BookmarkValue, bookmarkValueArgument);

            metadata.AddArgument(bookmarkValueArgument);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (!String.IsNullOrEmpty(this.BookmarkName))
            {
                context.CreateBookmark(this.BookmarkName, new BookmarkCallback(OnResumeBookmark));
            }
        }

        protected void OnResumeBookmark(NativeActivityContext context, Bookmark bookmark, object obj)
        {
            TestTraceListenerExtension listenerExtension = context.GetExtension<TestTraceListenerExtension>();
            Guid instanceId = context.WorkflowInstanceId;
            UserTrace.Trace(listenerExtension, instanceId, BeforeWait);
            Thread.CurrentThread.Join((int)WaitTime.TotalMilliseconds);
            if (!(obj is T))
            {
                throw new Exception("Resume Bookmark with object of type " + typeof(T).FullName);
            }

            BookmarkValue.Set(context, (T)obj);
            UserTrace.Trace(listenerExtension, instanceId, AfterWait);
        }

        protected override bool CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }

    public class TestWaitReadLine<T> : TestActivity
    {
        private WaitReadLine<T> _productReadLine;

        public TestWaitReadLine(string bookMarkName, string displayName)
        {
            _productReadLine = new WaitReadLine<T>(bookMarkName);
            this.ProductActivity = _productReadLine;
            this.DisplayName = displayName;
        }

        public TestWaitReadLine() :
            this(String.Empty, Guid.NewGuid().ToString())
        {
        }

        public string BookmarkName
        {
            get
            {
                return _productReadLine.BookmarkName;
            }
            set
            {
                _productReadLine.BookmarkName = value;
            }
        }

        public Activity<Location<T>> BookmarkValue
        {
            get
            {
                return _productReadLine.BookmarkValue.Expression;
            }
            set
            {
                _productReadLine.BookmarkValue = new OutArgument<T>(value);
            }
        }

        public TimeSpan WaitTime
        {
            get
            {
                return _productReadLine.WaitTime;
            }
            set
            {
                _productReadLine.WaitTime = value;
            }
        }

        protected override void GetActivitySpecificTrace(TraceGroup traceGroup)
        {
            traceGroup.Steps.Add(new UserTrace(WaitReadLine<T>.BeforeWait));
            traceGroup.Steps.Add(new UserTrace(WaitReadLine<T>.AfterWait));
        }
    }
}
