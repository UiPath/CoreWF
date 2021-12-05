// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;

public abstract class DelegateInArgument : DelegateArgument
{
    internal DelegateInArgument()
        : base() => Direction = ArgumentDirection.In;
}

public sealed class DelegateInArgument<T> : DelegateInArgument
{
    public DelegateInArgument()
        : base() { }

    public DelegateInArgument(string name)
        : base() => Name = name;

    protected override Type TypeCore => typeof(T);

    // Soft-Link: This method is referenced through reflection by
    // ExpressionUtilities.TryRewriteLambdaExpression.  Update that
    // file if the signature changes.
    public new T Get(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        return context.GetValue<T>((LocationReference)this);
    }

    public void Set(ActivityContext context, T value)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        context.SetValue((LocationReference)this, value);
    }

    internal override Location CreateLocation() => new Location<T>();
}
