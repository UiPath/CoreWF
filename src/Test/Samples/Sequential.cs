// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using CoreWf;
using CoreWf.Expressions;
using CoreWf.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Xunit;

namespace Samples
{
    /// <summary>
    /// Sample sequential workflow based on https://msdn.microsoft.com/en-us/library/gg983473.aspx
    /// </summary>
    public sealed class SequentialNumberGuess : Activity
    {
        public InArgument<int> GuessNumber { get; set; }
        public OutArgument<int> Turns { get; set; }
        private Activity GetImplementation()
        {
            var target = new Variable<int>();
            var guess = new Variable<int>();
            return new Sequence
            {
                Variables = { target, guess },
                Activities = {
                    new Assign<int> {
                        To = new OutArgument<int>(target),
                        Value = new InArgument<int>(ctx => GuessNumber.Get(ctx))
                    },
                    new DoWhile {
                        Body = new Sequence {
                            Activities = {
                                new Prompt {
                                    BookmarkName = "EnterGuess",
                                    Text = new InArgument<string>(ctx =>
                                        "Please enter your guess"),
                                    Result = new OutArgument<int>(guess)
                                },
                                new Assign<int> {
                                    To = new OutArgument<int>(ctx => Turns.Get(ctx)),
                                    Value = new InArgument<int>(ctx => Turns.Get(ctx) + 1)
                                },
                                new If {
                                    Condition = new InArgument<bool>(ctx => guess.Get(ctx) != target.Get(ctx)),
                                    Then = new If {
                                        Condition = new InArgument<bool>(ctx => guess.Get(ctx) < target.Get(ctx)),
                                        Then = new WriteLine { Text = "Your guess is too low."},
                                        Else = new WriteLine { Text = "Your guess is too high."}
                                    }
                                }
                            }
                        },
                        Condition = new LambdaValue<bool>(ctx => guess.Get(ctx) != target.Get(ctx))
                    }
                }
            };
        }
        private Func<Activity> _implementation;
        protected override Func<Activity> Implementation
        {
            get
            {
                return _implementation ?? (_implementation = GetImplementation);
            }
            set { throw new NotSupportedException(); }
        }

        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("GuessNumber", typeof(int), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Turns", typeof(int), ArgumentDirection.Out));
            metadata.Bind(this.GuessNumber, runtimeArguments[0]);
            metadata.Bind(this.Turns, runtimeArguments[1]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }
    }

    public sealed class Prompt : Activity<int>
    {
        public InArgument<string> BookmarkName { get; set; }
        public InArgument<string> Text { get; set; }
        private Activity GetImplementation()
        {
            return new Sequence
            {
                Activities = {
                    new WriteLine {
                        Text = new InArgument<string>(ctx => Text.Get(ctx))
                    },
                    new ReadInt {
                        BookmarkName = new InArgument<string>(ctx => BookmarkName.Get(ctx)),
                        Result = new OutArgument<int>(ctx => Result.Get(ctx))
                    }
                }
            };
        }
        private Func<Activity> _implementation;
        protected override Func<Activity> Implementation
        {
            get
            {
                return _implementation ?? (_implementation = GetImplementation);
            }
            set { throw new NotSupportedException(); }
        }

        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("BookmarkName", typeof(string), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Text", typeof(string), ArgumentDirection.In, true));
            metadata.Bind(this.BookmarkName, runtimeArguments[0]);
            metadata.Bind(this.Text, runtimeArguments[1]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }
    }

    public sealed class ReadInt : NativeActivity<int>
    {
        public InArgument<string> BookmarkName { get; set; }
        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark(BookmarkName.Get(context), OnReadComplete);
        }
        protected override bool CanInduceIdle { get { return true; } }
        private void OnReadComplete(NativeActivityContext context, Bookmark bookmark, object state)
        {
            Result.Set(context, (int)state);
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("BookmarkName", typeof(string), ArgumentDirection.In, true));
            metadata.Bind(this.BookmarkName, runtimeArguments[0]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }
    }

    public class Sequential : IDisposable
    {
        private const int ActualGuess = 5;
        private AutoResetEvent _completedEvent;
        private AutoResetEvent _idleEvent;
        private AutoResetEvent _failedEvent;
        private WorkflowApplication _wfApp;
        private bool _isComplete = false;

        public Sequential()
        {
            _completedEvent = new AutoResetEvent(false);
            _idleEvent = new AutoResetEvent(false);
            _failedEvent = new AutoResetEvent(false);
            _wfApp = new WorkflowApplication(new SequentialNumberGuess(),
                new Dictionary<string, object>() { { "GuessNumber", ActualGuess } });
            _wfApp.Completed = (WorkflowApplicationCompletedEventArgs e) =>
            {
                _isComplete = true;
                _completedEvent.Set();
            };
            _wfApp.Aborted = (WorkflowApplicationAbortedEventArgs e) =>
            {
                _failedEvent.Set();
            };
            _wfApp.OnUnhandledException = (WorkflowApplicationUnhandledExceptionEventArgs e) =>
            {
                Console.WriteLine(e.UnhandledException.ToString());
                return UnhandledExceptionAction.Terminate;
            };
            _wfApp.Idle = (WorkflowApplicationIdleEventArgs e) =>
            {
                _idleEvent.Set();
            };
            _wfApp.Run();
        }

        [Fact]
        public void GuessCorrectNumber()
        {
            _wfApp.ResumeBookmark("EnterGuess", ActualGuess);
            Assert.True(_completedEvent.WaitOne(500));
            Assert.True(_isComplete);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void GuessWrongNumber(int guess)
        {
            _wfApp.ResumeBookmark("EnterGuess", guess);
            _idleEvent.WaitOne(500);
            Assert.False(_isComplete);
        }

        public void Dispose()
        {
            if (!_isComplete && _wfApp != null)
            {
                _wfApp.Abort();
            }
        }
    }
}
