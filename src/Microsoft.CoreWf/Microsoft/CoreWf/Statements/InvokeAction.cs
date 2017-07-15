// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CoreWf.Statements
{
    //[ContentProperty("Action")]
    public sealed class InvokeAction : NativeActivity
    {
        private IList<Argument> _actionArguments;

        public InvokeAction()
        {
            _actionArguments = new ValidatingCollection<Argument>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                    }
                }
            };
        }

        [DefaultValue(null)]
        public ActivityAction Action
        {
            get;
            set;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action);
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T> Argument
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            RuntimeArgument runtimeArgument = new RuntimeArgument("Argument", typeof(T), ArgumentDirection.In, true);
            metadata.Bind(this.Argument, runtimeArgument);

            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { runtimeArgument });
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null) // no-op
            {
                return;
            }

            context.ScheduleAction<T>(Action, Argument.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null) // no-op
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T9> Argument9
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument9", typeof(T9), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);
            metadata.Bind(this.Argument9, runtimeArguments[8]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context), Argument9.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T9> Argument9
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T10> Argument10
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument9", typeof(T9), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument10", typeof(T10), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);
            metadata.Bind(this.Argument9, runtimeArguments[8]);
            metadata.Bind(this.Argument10, runtimeArguments[9]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context), Argument9.Get(context), Argument10.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T9> Argument9
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T10> Argument10
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T11> Argument11
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument9", typeof(T9), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument10", typeof(T10), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument11", typeof(T11), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);
            metadata.Bind(this.Argument9, runtimeArguments[8]);
            metadata.Bind(this.Argument10, runtimeArguments[9]);
            metadata.Bind(this.Argument11, runtimeArguments[10]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T9> Argument9
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T10> Argument10
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T11> Argument11
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T12> Argument12
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument9", typeof(T9), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument10", typeof(T10), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument11", typeof(T11), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument12", typeof(T12), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);
            metadata.Bind(this.Argument9, runtimeArguments[8]);
            metadata.Bind(this.Argument10, runtimeArguments[9]);
            metadata.Bind(this.Argument11, runtimeArguments[10]);
            metadata.Bind(this.Argument12, runtimeArguments[11]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
                Argument12.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T9> Argument9
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T10> Argument10
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T11> Argument11
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T12> Argument12
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T13> Argument13
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument9", typeof(T9), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument10", typeof(T10), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument11", typeof(T11), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument12", typeof(T12), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument13", typeof(T13), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);
            metadata.Bind(this.Argument9, runtimeArguments[8]);
            metadata.Bind(this.Argument10, runtimeArguments[9]);
            metadata.Bind(this.Argument11, runtimeArguments[10]);
            metadata.Bind(this.Argument12, runtimeArguments[11]);
            metadata.Bind(this.Argument13, runtimeArguments[12]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
                Argument12.Get(context), Argument13.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T9> Argument9
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T10> Argument10
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T11> Argument11
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T12> Argument12
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T13> Argument13
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T14> Argument14
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument9", typeof(T9), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument10", typeof(T10), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument11", typeof(T11), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument12", typeof(T12), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument13", typeof(T13), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument14", typeof(T14), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);
            metadata.Bind(this.Argument9, runtimeArguments[8]);
            metadata.Bind(this.Argument10, runtimeArguments[9]);
            metadata.Bind(this.Argument11, runtimeArguments[10]);
            metadata.Bind(this.Argument12, runtimeArguments[11]);
            metadata.Bind(this.Argument13, runtimeArguments[12]);
            metadata.Bind(this.Argument14, runtimeArguments[13]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
                Argument12.Get(context), Argument13.Get(context), Argument14.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T9> Argument9
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T10> Argument10
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T11> Argument11
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T12> Argument12
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T13> Argument13
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T14> Argument14
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T15> Argument15
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument9", typeof(T9), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument10", typeof(T10), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument11", typeof(T11), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument12", typeof(T12), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument13", typeof(T13), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument14", typeof(T14), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument15", typeof(T15), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);
            metadata.Bind(this.Argument9, runtimeArguments[8]);
            metadata.Bind(this.Argument10, runtimeArguments[9]);
            metadata.Bind(this.Argument11, runtimeArguments[10]);
            metadata.Bind(this.Argument12, runtimeArguments[11]);
            metadata.Bind(this.Argument13, runtimeArguments[12]);
            metadata.Bind(this.Argument14, runtimeArguments[13]);
            metadata.Bind(this.Argument15, runtimeArguments[14]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
                Argument12.Get(context), Argument13.Get(context), Argument14.Get(context), Argument15.Get(context));
        }
    }

    //[ContentProperty("Action")]
    public sealed class InvokeAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : NativeActivity
    {
        public InvokeAction()
        {
        }

        [RequiredArgument]
        public InArgument<T1> Argument1
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T2> Argument2
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T3> Argument3
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T4> Argument4
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T5> Argument5
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T6> Argument6
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T7> Argument7
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T8> Argument8
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T9> Argument9
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T10> Argument10
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T11> Argument11
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T12> Argument12
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T13> Argument13
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T14> Argument14
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T15> Argument15
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<T16> Argument16
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public ActivityAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Action
        {
            get;
            set;
        }

        //protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        //{
        //    metadata.AllowUpdateInsideThisActivity();
        //}

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(this.Action);

            var runtimeArguments = new Collection<RuntimeArgument>();
            runtimeArguments.Add(new RuntimeArgument("Argument1", typeof(T1), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument2", typeof(T2), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument3", typeof(T3), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument4", typeof(T4), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument5", typeof(T5), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument6", typeof(T6), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument7", typeof(T7), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument8", typeof(T8), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument9", typeof(T9), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument10", typeof(T10), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument11", typeof(T11), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument12", typeof(T12), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument13", typeof(T13), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument14", typeof(T14), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument15", typeof(T15), ArgumentDirection.In, true));
            runtimeArguments.Add(new RuntimeArgument("Argument16", typeof(T16), ArgumentDirection.In, true));
            metadata.Bind(this.Argument1, runtimeArguments[0]);
            metadata.Bind(this.Argument2, runtimeArguments[1]);
            metadata.Bind(this.Argument3, runtimeArguments[2]);
            metadata.Bind(this.Argument4, runtimeArguments[3]);
            metadata.Bind(this.Argument5, runtimeArguments[4]);
            metadata.Bind(this.Argument6, runtimeArguments[5]);
            metadata.Bind(this.Argument7, runtimeArguments[6]);
            metadata.Bind(this.Argument8, runtimeArguments[7]);
            metadata.Bind(this.Argument9, runtimeArguments[8]);
            metadata.Bind(this.Argument10, runtimeArguments[9]);
            metadata.Bind(this.Argument11, runtimeArguments[10]);
            metadata.Bind(this.Argument12, runtimeArguments[11]);
            metadata.Bind(this.Argument13, runtimeArguments[12]);
            metadata.Bind(this.Argument14, runtimeArguments[13]);
            metadata.Bind(this.Argument15, runtimeArguments[14]);
            metadata.Bind(this.Argument16, runtimeArguments[15]);

            metadata.SetArgumentsCollection(runtimeArguments);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Action == null || Action.Handler == null)
            {
                return;
            }

            context.ScheduleAction(Action, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
                Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
                Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
                Argument12.Get(context), Argument13.Get(context), Argument14.Get(context), Argument15.Get(context),
                Argument16.Get(context));
        }
    }
}


