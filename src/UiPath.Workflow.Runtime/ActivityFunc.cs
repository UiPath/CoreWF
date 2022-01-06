// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;

public sealed class ActivityFunc<TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument()
    {
        return Result;
    }

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(1)
        {
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };

        return parameters;
    }
}

public sealed class ActivityFunc<T, TResult> : ActivityDelegate
{
    public ActivityFunc()
    {
    }

    [DefaultValue(null)]
    public DelegateInArgument<T> Argument { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(2)
        {
            { new RuntimeDelegateArgument(ArgumentName, typeof(T), ArgumentDirection.In, Argument) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(3)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(4)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(5)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(6)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(7)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(8)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(9)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(10)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(Argument9Name, typeof(T9), ArgumentDirection.In, Argument9) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T10> Argument10 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(11)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(Argument9Name, typeof(T9), ArgumentDirection.In, Argument9) },
            { new RuntimeDelegateArgument(Argument10Name, typeof(T10), ArgumentDirection.In, Argument10) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T10> Argument10 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T11> Argument11 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(12)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(Argument9Name, typeof(T9), ArgumentDirection.In, Argument9) },
            { new RuntimeDelegateArgument(Argument10Name, typeof(T10), ArgumentDirection.In, Argument10) },
            { new RuntimeDelegateArgument(Argument11Name, typeof(T11), ArgumentDirection.In, Argument11) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T10> Argument10 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T11> Argument11 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T12> Argument12 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(13)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(Argument9Name, typeof(T9), ArgumentDirection.In, Argument9) },
            { new RuntimeDelegateArgument(Argument10Name, typeof(T10), ArgumentDirection.In, Argument10) },
            { new RuntimeDelegateArgument(Argument11Name, typeof(T11), ArgumentDirection.In, Argument11) },
            { new RuntimeDelegateArgument(Argument12Name, typeof(T12), ArgumentDirection.In, Argument12) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T10> Argument10 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T11> Argument11 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T12> Argument12 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T13> Argument13 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(14)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(Argument9Name, typeof(T9), ArgumentDirection.In, Argument9) },
            { new RuntimeDelegateArgument(Argument10Name, typeof(T10), ArgumentDirection.In, Argument10) },
            { new RuntimeDelegateArgument(Argument11Name, typeof(T11), ArgumentDirection.In, Argument11) },
            { new RuntimeDelegateArgument(Argument12Name, typeof(T12), ArgumentDirection.In, Argument12) },
            { new RuntimeDelegateArgument(Argument13Name, typeof(T13), ArgumentDirection.In, Argument13) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T10> Argument10 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T11> Argument11 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T12> Argument12 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T13> Argument13 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T14> Argument14 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(15)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(Argument9Name, typeof(T9), ArgumentDirection.In, Argument9) },
            { new RuntimeDelegateArgument(Argument10Name, typeof(T10), ArgumentDirection.In, Argument10) },
            { new RuntimeDelegateArgument(Argument11Name, typeof(T11), ArgumentDirection.In, Argument11) },
            { new RuntimeDelegateArgument(Argument12Name, typeof(T12), ArgumentDirection.In, Argument12) },
            { new RuntimeDelegateArgument(Argument13Name, typeof(T13), ArgumentDirection.In, Argument13) },
            { new RuntimeDelegateArgument(Argument14Name, typeof(T14), ArgumentDirection.In, Argument14) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T10> Argument10 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T11> Argument11 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T12> Argument12 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T13> Argument13 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T14> Argument14 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T15> Argument15 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(16)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(Argument9Name, typeof(T9), ArgumentDirection.In, Argument9) },
            { new RuntimeDelegateArgument(Argument10Name, typeof(T10), ArgumentDirection.In, Argument10) },
            { new RuntimeDelegateArgument(Argument11Name, typeof(T11), ArgumentDirection.In, Argument11) },
            { new RuntimeDelegateArgument(Argument12Name, typeof(T12), ArgumentDirection.In, Argument12) },
            { new RuntimeDelegateArgument(Argument13Name, typeof(T13), ArgumentDirection.In, Argument13) },
            { new RuntimeDelegateArgument(Argument14Name, typeof(T14), ArgumentDirection.In, Argument14) },
            { new RuntimeDelegateArgument(Argument15Name, typeof(T15), ArgumentDirection.In, Argument15) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}

public sealed class ActivityFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> : ActivityDelegate
{
    public ActivityFunc() { }

    [DefaultValue(null)]
    public DelegateInArgument<T1> Argument1 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T2> Argument2 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T3> Argument3 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T4> Argument4 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T5> Argument5 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T6> Argument6 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T7> Argument7 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T8> Argument8 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T9> Argument9 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T10> Argument10 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T11> Argument11 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T12> Argument12 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T13> Argument13 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T14> Argument14 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T15> Argument15 { get; set; }

    [DefaultValue(null)]
    public DelegateInArgument<T16> Argument16 { get; set; }

    [DefaultValue(null)]
    public DelegateOutArgument<TResult> Result { get; set; }

    protected internal override DelegateOutArgument GetResultArgument() => Result;

    internal override IList<RuntimeDelegateArgument> InternalGetRuntimeDelegateArguments()
    {
        IList<RuntimeDelegateArgument> parameters = new List<RuntimeDelegateArgument>(17)
        {
            { new RuntimeDelegateArgument(Argument1Name, typeof(T1), ArgumentDirection.In, Argument1) },
            { new RuntimeDelegateArgument(Argument2Name, typeof(T2), ArgumentDirection.In, Argument2) },
            { new RuntimeDelegateArgument(Argument3Name, typeof(T3), ArgumentDirection.In, Argument3) },
            { new RuntimeDelegateArgument(Argument4Name, typeof(T4), ArgumentDirection.In, Argument4) },
            { new RuntimeDelegateArgument(Argument5Name, typeof(T5), ArgumentDirection.In, Argument5) },
            { new RuntimeDelegateArgument(Argument6Name, typeof(T6), ArgumentDirection.In, Argument6) },
            { new RuntimeDelegateArgument(Argument7Name, typeof(T7), ArgumentDirection.In, Argument7) },
            { new RuntimeDelegateArgument(Argument8Name, typeof(T8), ArgumentDirection.In, Argument8) },
            { new RuntimeDelegateArgument(Argument9Name, typeof(T9), ArgumentDirection.In, Argument9) },
            { new RuntimeDelegateArgument(Argument10Name, typeof(T10), ArgumentDirection.In, Argument10) },
            { new RuntimeDelegateArgument(Argument11Name, typeof(T11), ArgumentDirection.In, Argument11) },
            { new RuntimeDelegateArgument(Argument12Name, typeof(T12), ArgumentDirection.In, Argument12) },
            { new RuntimeDelegateArgument(Argument13Name, typeof(T13), ArgumentDirection.In, Argument13) },
            { new RuntimeDelegateArgument(Argument14Name, typeof(T14), ArgumentDirection.In, Argument14) },
            { new RuntimeDelegateArgument(Argument15Name, typeof(T15), ArgumentDirection.In, Argument15) },
            { new RuntimeDelegateArgument(Argument16Name, typeof(T16), ArgumentDirection.In, Argument16) },
            { new RuntimeDelegateArgument(ResultArgumentName, typeof(TResult), ArgumentDirection.Out, Result) }
        };
        return parameters;
    }
}
