// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Hosting;

internal abstract class WorkflowInstanceExtensionProvider
{
    protected WorkflowInstanceExtensionProvider() { }

    public Type Type { get; protected set; }

    protected bool GeneratedTypeMatchesDeclaredType { get; set; }

    public abstract object ProvideValue();

    public bool IsMatch<TTarget>(object value)
        where TTarget : class
    {
        Fx.Assert(value != null, "extension providers never return a null extension");
        return value is TTarget && (GeneratedTypeMatchesDeclaredType || TypeHelper.AreReferenceTypesCompatible(Type, typeof(TTarget)));
    }
}

internal class WorkflowInstanceExtensionProvider<T> : WorkflowInstanceExtensionProvider
    where T : class
{
    private readonly Func<T> _providerFunction;
    private bool _hasGeneratedValue;

    public WorkflowInstanceExtensionProvider(Func<T> providerFunction)
        : base()
    {
        _providerFunction = providerFunction;
        Type = typeof(T);
    }

    public override object ProvideValue()
    {
        T value = _providerFunction();
        if (!_hasGeneratedValue)
        {
            GeneratedTypeMatchesDeclaredType = ReferenceEquals(value.GetType(), Type);
            _hasGeneratedValue = true;
        }

        return value;
    }
}
