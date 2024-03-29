// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Windows.Markup;

#if DYNAMICUPDATE
using System.Activities.DynamicUpdate;
#endif

namespace System.Activities.Expressions;

[ContentProperty("Func")]
public sealed class InvokeFunc<TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [DefaultValue(null)]
    public ActivityFunc<TResult> Func { get; set; }

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }
        context.ScheduleFunc(Func,
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
        else
        {
            Result.Set(context, default);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T> Argument { get; set; }

    public ActivityFunc<T, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }
        context.ScheduleFunc(Func,
            Argument.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
        else
        {
            Result.Set(context, default);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func,
            Argument1.Get(context),
            Argument2.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func,
            Argument1.Get(context),
            Argument2.Get(context),
            Argument3.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func,
            Argument1.Get(context),
            Argument2.Get(context),
            Argument3.Get(context),
            Argument4.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [RequiredArgument]
    public InArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context), Argument9.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [RequiredArgument]
    public InArgument<T9> Argument9 { get; set; }

    [RequiredArgument]
    public InArgument<T10> Argument10 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context), Argument9.Get(context), Argument10.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [RequiredArgument]
    public InArgument<T9> Argument9 { get; set; }

    [RequiredArgument]
    public InArgument<T10> Argument10 { get; set; }

    [RequiredArgument]
    public InArgument<T11> Argument11 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [RequiredArgument]
    public InArgument<T9> Argument9 { get; set; }

    [RequiredArgument]
    public InArgument<T10> Argument10 { get; set; }

    [RequiredArgument]
    public InArgument<T11> Argument11 { get; set; }

    [RequiredArgument]
    public InArgument<T12> Argument12 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
            Argument12.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [RequiredArgument]
    public InArgument<T9> Argument9 { get; set; }

    [RequiredArgument]
    public InArgument<T10> Argument10 { get; set; }

    [RequiredArgument]
    public InArgument<T11> Argument11 { get; set; }

    [RequiredArgument]
    public InArgument<T12> Argument12 { get; set; }

    [RequiredArgument]
    public InArgument<T13> Argument13 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
            Argument12.Get(context), Argument13.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [RequiredArgument]
    public InArgument<T9> Argument9 { get; set; }

    [RequiredArgument]
    public InArgument<T10> Argument10 { get; set; }

    [RequiredArgument]
    public InArgument<T11> Argument11 { get; set; }

    [RequiredArgument]
    public InArgument<T12> Argument12 { get; set; }

    [RequiredArgument]
    public InArgument<T13> Argument13 { get; set; }

    [RequiredArgument]
    public InArgument<T14> Argument14 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
            Argument12.Get(context), Argument13.Get(context), Argument14.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [RequiredArgument]
    public InArgument<T9> Argument9 { get; set; }

    [RequiredArgument]
    public InArgument<T10> Argument10 { get; set; }

    [RequiredArgument]
    public InArgument<T11> Argument11 { get; set; }

    [RequiredArgument]
    public InArgument<T12> Argument12 { get; set; }

    [RequiredArgument]
    public InArgument<T13> Argument13 { get; set; }

    [RequiredArgument]
    public InArgument<T14> Argument14 { get; set; }

    [RequiredArgument]
    public InArgument<T15> Argument15 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
            Argument12.Get(context), Argument13.Get(context), Argument14.Get(context), Argument15.Get(context),
            new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}

[ContentProperty("Func")]
public sealed class InvokeFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> : NativeActivity<TResult>
{
    public InvokeFunc() { }

    [RequiredArgument]
    public InArgument<T1> Argument1 { get; set; }

    [RequiredArgument]
    public InArgument<T2> Argument2 { get; set; }

    [RequiredArgument]
    public InArgument<T3> Argument3 { get; set; }

    [RequiredArgument]
    public InArgument<T4> Argument4 { get; set; }

    [RequiredArgument]
    public InArgument<T5> Argument5 { get; set; }

    [RequiredArgument]
    public InArgument<T6> Argument6 { get; set; }

    [RequiredArgument]
    public InArgument<T7> Argument7 { get; set; }

    [RequiredArgument]
    public InArgument<T8> Argument8 { get; set; }

    [RequiredArgument]
    public InArgument<T9> Argument9 { get; set; }

    [RequiredArgument]
    public InArgument<T10> Argument10 { get; set; }

    [RequiredArgument]
    public InArgument<T11> Argument11 { get; set; }

    [RequiredArgument]
    public InArgument<T12> Argument12 { get; set; }

    [RequiredArgument]
    public InArgument<T13> Argument13 { get; set; }

    [RequiredArgument]
    public InArgument<T14> Argument14 { get; set; }

    [RequiredArgument]
    public InArgument<T15> Argument15 { get; set; }

    [RequiredArgument]
    public InArgument<T16> Argument16 { get; set; }

    [DefaultValue(null)]
    public ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> Func { get; set; }

#if DYNAMICUPDATE
    protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
    {
        metadata.AllowUpdateInsideThisActivity();
    } 
#endif

    protected override void Execute(NativeActivityContext context)
    {
        if (Func == null || Func.Handler == null)
        {
            return;
        }

        context.ScheduleFunc(Func, Argument1.Get(context), Argument2.Get(context), Argument3.Get(context),
            Argument4.Get(context), Argument5.Get(context), Argument6.Get(context), Argument7.Get(context),
            Argument8.Get(context), Argument9.Get(context), Argument10.Get(context), Argument11.Get(context),
            Argument12.Get(context), Argument13.Get(context), Argument14.Get(context), Argument15.Get(context),
            Argument16.Get(context), new CompletionCallback<TResult>(OnActivityFuncComplete));
    }

    private void OnActivityFuncComplete(NativeActivityContext context, ActivityInstance completedInstance, TResult resultValue)
    {
        if (completedInstance.State == ActivityInstanceState.Closed)
        {
            Result.Set(context, resultValue);
        }
    }
}
