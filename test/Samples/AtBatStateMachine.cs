// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CoreWf;
using Microsoft.CoreWf.Expressions;
using Microsoft.CoreWf.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Samples
{
    public class AtBatStateMachine : IDisposable
    {
        private AutoResetEvent _completedEvent;
        private AutoResetEvent _idleEvent;
        private WorkflowApplication _wfApp;
        private int _pitchCount = -1;
        private string _atBatResult = "invalid";

        public AtBatStateMachine()
        {
        }

        private WorkflowApplication CreateWorkflowApplication(Activity workflowDefinition, AutoResetEvent idleEvent, AutoResetEvent completedEvent)
        {
            WorkflowApplication wfApp = Utilities.CreateWorkflowApplication(
                workflowDefinition,
                /*store=*/ null,
                idleEvent,
                /*unloadedEvent=*/ null,
                completedEvent,
                /*abortedEvent=*/ null,
                /*wfArguments=*/ null,
                delegate (WorkflowApplicationIdleEventArgs e)
                {
                    Console.WriteLine("Workflow idled");
                    idleEvent.Set();
                },
                /*unloadedDelegate=*/ null,
                delegate (WorkflowApplicationCompletedEventArgs e)
                {
                    Console.WriteLine("Workflow completed with state {0}.", e.CompletionState.ToString());
                    if (e.TerminationException != null)
                    {
                        Console.WriteLine("TerminationException = {0}; {1}", e.TerminationException.GetType().ToString(), e.TerminationException.Message);
                    }
                    else
                    {
                        object value;
                        if (!e.Outputs.TryGetValue("AtBatResult", out value))
                        {
                            throw new Exception("Did not find AtBatResult in output from workflow");
                        }
                        _atBatResult = (string)value;

                        if (!e.Outputs.TryGetValue("PitchCount", out value))
                        {
                            throw new Exception("Did not find PitchCount in output from workflow");
                        }
                        _pitchCount = (int)value;
                        Console.WriteLine("AtBatResult = {0}", (_atBatResult != null ? _atBatResult : "null"));
                        Console.WriteLine("PitchCount = {0}", _pitchCount.ToString());
                    }
                    completedEvent.Set();
                }
                );

            return wfApp;
        }

        [Fact]
        public void NextBatter()
        {
            _completedEvent = new AutoResetEvent(false);
            _idleEvent = new AutoResetEvent(false);

            AutoResetEvent[] events = new AutoResetEvent[2] { _idleEvent, _completedEvent };
            Random random = new Random();

            Activity workflow = new OuterActivity();

            WorkflowApplication app = CreateWorkflowApplication(workflow, _idleEvent, _completedEvent);
            app.Run();
            while (true)
            {
                int pitchInt = random.Next(1, 5);
                string pitch;
                switch (pitchInt)
                {
                    case 1: { pitch = "Ball"; break; }
                    case 2: { pitch = "Strike"; break; }
                    case 3: { pitch = "Foul"; break; }
                    case 4: { pitch = "Hit"; break; }
                    case 5: { pitch = "FieldingOut"; break; }
                    default: { pitch = "Ball"; break; }
                };
                int eventIndex = WaitHandle.WaitAny(events, TimeSpan.FromSeconds(2));
                if (eventIndex == 1)
                {
                    break;
                }
                app.ResumeBookmark(pitch, null);
            }

            Assert.True(_pitchCount > 0);
            Assert.True((_atBatResult != null) && (_atBatResult != "invalid"));
        }

        public void Dispose()
        {
        }
    }

    internal sealed class OuterActivity : Activity
    {
        public OutArgument<string> AtBatResult { get; set; }
        public OutArgument<int> PitchCount { get; set; }

        private Activity GetImplementation()
        {
            Variable<string> atBatResult = new Variable<string>
            {
                Name = "outerAtBatResult",
                Default = "default at bat result"
            };
            Variable<int> pitchCount = new Variable<int>
            {
                Name = "outerPitchCount",
                Default = 0
            };

            Activity stateMachine = CreateStateMachine(atBatResult, pitchCount);

            return new Sequence
            {
                DisplayName = "OuterImplementation",
                Variables = { atBatResult, pitchCount },
                Activities =
                {
                    stateMachine,
                    new Assign<string>
                    {
                        To = new OutArgument<string>(ctx => AtBatResult.Get(ctx)),
                        Value = new InArgument<string>(ctx => atBatResult.Get(ctx))
                    },
                    new Assign<int>
                    {
                        To = new OutArgument<int>(ctx => PitchCount.Get(ctx)),
                        Value = new InArgument<int>(ctx => pitchCount.Get(ctx))
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
            set
            {
                throw new NotSupportedException();
            }
        }

        protected override void CacheMetadata(ActivityMetadata metadata)
        {
            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("AtBatResult", typeof(string), ArgumentDirection.Out));
            runtimeArguments.Add(new RuntimeArgument("PitchCount", typeof(int), ArgumentDirection.Out));
            metadata.Bind(this.AtBatResult, runtimeArguments[0]);
            metadata.Bind(this.PitchCount, runtimeArguments[1]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        private Activity CreateStateMachine(Variable<string> atBatOutcome, Variable<int> pitchCount)
        {
            State batterUp = new State
            {
                DisplayName = "batterUp",
                IsFinal = false,
            };

            State oneAndOh = new State
            {
                DisplayName = "oneAndOh",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State twoAndOh = new State
            {
                DisplayName = "twoAndOh",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State threeAndOh = new State
            {
                DisplayName = "threeAndOh",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State ohAndOne = new State
            {
                DisplayName = "ohAndOne",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State ohAndTwo = new State
            {
                DisplayName = "ohAndTwo",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State oneAndOne = new State
            {
                DisplayName = "oneAndOne",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State oneAndTwo = new State
            {
                DisplayName = "oneAndTwo",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State twoAndOne = new State
            {
                DisplayName = "twoAndOne",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State twoAndTwo = new State
            {
                DisplayName = "twoAndTwo",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State threeAndOne = new State
            {
                DisplayName = "threeAndOne",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State threeAndTwo = new State
            {
                DisplayName = "threeAndTwo",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State strikeOut = new State
            {
                DisplayName = "strikeOut",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State walk = new State
            {
                DisplayName = "walk",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State hit = new State
            {
                DisplayName = "hit",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State fieldingOut = new State
            {
                DisplayName = "fieldingOut",
                IsFinal = false,
                Entry = new Assign
                {
                    To = new OutArgument<int>(pitchCount),
                    Value = new InArgument<int>(ctx => pitchCount.Get(ctx) + 1)
                }
            };

            State allDone = new State
            {
                DisplayName = "allDone",
                IsFinal = true,
            };


            StateMachine wf = new StateMachine();
            wf.InitialState = batterUp;
            wf.States.Add(batterUp);
            wf.States.Add(oneAndOh);
            wf.States.Add(twoAndOh);
            wf.States.Add(threeAndOh);
            wf.States.Add(walk);
            wf.States.Add(ohAndOne);
            wf.States.Add(ohAndTwo);
            wf.States.Add(strikeOut);
            wf.States.Add(oneAndOne);
            wf.States.Add(oneAndTwo);
            wf.States.Add(twoAndOne);
            wf.States.Add(twoAndTwo);
            wf.States.Add(threeAndOne);
            wf.States.Add(threeAndTwo);
            wf.States.Add(hit);
            wf.States.Add(fieldingOut);
            wf.States.Add(allDone);

            AddTransition(batterUp, "Ball", oneAndOh);
            AddTransition(batterUp, "Strike", ohAndOne);
            AddTransition(batterUp, "Foul", ohAndOne);
            AddTransition(batterUp, "Hit", hit);
            AddTransition(batterUp, "Out", fieldingOut);

            AddTransition(oneAndOh, "Ball", twoAndOh);
            AddTransition(oneAndOh, "Strike", oneAndOne);
            AddTransition(oneAndOh, "Foul", oneAndOne);
            AddTransition(oneAndOh, "Hit", hit);
            AddTransition(oneAndOh, "Out", fieldingOut);

            AddTransition(twoAndOh, "Ball", threeAndOh);
            AddTransition(twoAndOh, "Strike", twoAndOne);
            AddTransition(twoAndOh, "Foul", twoAndOne);
            AddTransition(twoAndOh, "Hit", hit);
            AddTransition(twoAndOh, "Out", fieldingOut);

            AddTransition(threeAndOh, "Ball", walk);
            AddTransition(threeAndOh, "Strike", threeAndOne);
            AddTransition(threeAndOh, "Foul", threeAndOne);
            AddTransition(threeAndOh, "Hit", hit);
            AddTransition(threeAndOh, "Out", fieldingOut);

            AddTransition(oneAndOne, "Ball", twoAndOne);
            AddTransition(oneAndOne, "Strike", oneAndTwo);
            AddTransition(oneAndOne, "Foul", oneAndTwo);
            AddTransition(oneAndOne, "Hit", hit);
            AddTransition(oneAndOne, "Out", fieldingOut);

            AddTransition(twoAndOne, "Ball", threeAndOne);
            AddTransition(twoAndOne, "Strike", twoAndTwo);
            AddTransition(twoAndOne, "Foul", twoAndTwo);
            AddTransition(twoAndOne, "Hit", hit);
            AddTransition(twoAndOne, "Out", fieldingOut);

            AddTransition(threeAndOne, "Ball", walk);
            AddTransition(threeAndOne, "Strike", threeAndTwo);
            AddTransition(threeAndOne, "Foul", threeAndTwo);
            AddTransition(threeAndOne, "Hit", hit);
            AddTransition(threeAndOne, "Out", fieldingOut);

            AddTransition(oneAndTwo, "Ball", twoAndTwo);
            AddTransition(oneAndTwo, "Strike", strikeOut);
            AddTransition(oneAndTwo, "Foul", oneAndTwo);
            AddTransition(oneAndTwo, "Hit", hit);
            AddTransition(oneAndTwo, "Out", fieldingOut);

            AddTransition(twoAndTwo, "Ball", threeAndTwo);
            AddTransition(twoAndTwo, "Strike", strikeOut);
            AddTransition(twoAndTwo, "Foul", twoAndTwo);
            AddTransition(twoAndTwo, "Hit", hit);
            AddTransition(twoAndTwo, "Out", fieldingOut);

            AddTransition(threeAndTwo, "Ball", walk);
            AddTransition(threeAndTwo, "Strike", strikeOut);
            AddTransition(threeAndTwo, "Foul", threeAndTwo);
            AddTransition(threeAndTwo, "Hit", hit);
            AddTransition(threeAndTwo, "Out", fieldingOut);

            AddTransition(ohAndOne, "Ball", oneAndOne);
            AddTransition(ohAndOne, "Strike", ohAndTwo);
            AddTransition(ohAndOne, "Foul", ohAndTwo);
            AddTransition(ohAndOne, "Hit", hit);
            AddTransition(ohAndOne, "Out", fieldingOut);

            AddTransition(ohAndTwo, "Ball", oneAndTwo);
            AddTransition(ohAndTwo, "Strike", strikeOut);
            AddTransition(ohAndTwo, "Foul", oneAndTwo);
            AddTransition(ohAndTwo, "Hit", hit);
            AddTransition(ohAndTwo, "Out", fieldingOut);

            Transition tx = new Transition
            {
                To = allDone
            };
            hit.Transitions.Add(tx);
            hit.Exit = new Assign<string>
            {
                To = new OutArgument<string>(atBatOutcome),
                Value = new InArgument<string>("HIT!")
            };

            tx = new Transition
            {
                To = allDone
            };
            walk.Transitions.Add(tx);
            walk.Exit = new Assign<string>
            {
                To = new OutArgument<string>(atBatOutcome),
                Value = new InArgument<string>("Walk")
            };

            tx = new Transition
            {
                To = allDone
            };
            strikeOut.Transitions.Add(tx);
            strikeOut.Exit = new Assign<string>
            {
                To = new OutArgument<string>(atBatOutcome),
                Value = new InArgument<string>("Strikeout :(")
            };

            tx = new Transition
            {
                To = allDone
            };
            fieldingOut.Transitions.Add(tx);
            fieldingOut.Exit = new Assign<string>
            {
                To = new OutArgument<string>(atBatOutcome),
                Value = new InArgument<string>("Fielding Out")
            };


            return wf;
        }
        private void AddTransition(State currentState, string pitch, State nextState)
        {
            Transition tx = new Transition
            {
                Trigger = new BookmarkActivity
                {
                    BookmarkName = new InArgument<string>(pitch),
                    Options = BookmarkOptions.None
                },
                To = nextState
            };
            currentState.Transitions.Add(tx);
        }
    }
}
