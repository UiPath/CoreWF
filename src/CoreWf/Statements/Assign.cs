// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Collections.ObjectModel;

namespace System.Activities.Statements;

public sealed class Assign : CodeActivity
{
    public Assign()
        : base() { }

    [RequiredArgument]
    [DefaultValue(null)]
    public OutArgument To { get; set; }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument Value { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        Collection<RuntimeArgument> arguments = new();
            
        Type valueType = TypeHelper.ObjectType;

        if (Value != null)
        {
            valueType = Value.ArgumentType;
        }

        RuntimeArgument valueArgument = new("Value", valueType, ArgumentDirection.In, true);
        metadata.Bind(Value, valueArgument);

        Type toType = TypeHelper.ObjectType;

        if (To != null)
        {
            toType = To.ArgumentType;
        }

        RuntimeArgument toArgument = new("To", toType, ArgumentDirection.Out, true);
        metadata.Bind(To, toArgument);

        arguments.Add(valueArgument);
        arguments.Add(toArgument);

        metadata.SetArgumentsCollection(arguments);

        if (Value != null && To != null)
        {
            if (!TypeHelper.AreTypesCompatible(Value.ArgumentType, To.ArgumentType))
            {
                metadata.AddValidationError(SR.TypeMismatchForAssign(
                            Value.ArgumentType,
                            To.ArgumentType,
                            DisplayName));
            }
        }
    }

    protected override void Execute(CodeActivityContext context) => To.Set(context, Value.Get(context));
}

public sealed class Assign<T> : CodeActivity
{
    public Assign()
        : base() { }

    [RequiredArgument]
    [DefaultValue(null)]
    public OutArgument<T> To { get; set; }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<T> Value { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        Collection<RuntimeArgument> arguments = new();

        RuntimeArgument valueArgument = new("Value", typeof(T), ArgumentDirection.In, true);
        metadata.Bind(Value, valueArgument);

        RuntimeArgument toArgument = new("To", typeof(T), ArgumentDirection.Out, true);
        metadata.Bind(To, toArgument);

        arguments.Add(valueArgument);
        arguments.Add(toArgument);

        metadata.SetArgumentsCollection(arguments);
    }

    protected override void Execute(CodeActivityContext context) => context.SetValue(To, Value.Get(context));
}
