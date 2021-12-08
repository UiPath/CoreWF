// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Activities.Statements;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows.Markup;

namespace System.Activities.Expressions;

[ContentProperty("Parameters")]
public sealed class InvokeMethod<TResult> : AsyncCodeActivity<TResult>
{
    private Collection<Argument> _parameters;
    private Collection<Type> _genericTypeArguments;
    private MethodResolver _methodResolver;
    private MethodExecutor _methodExecutor;
    private RuntimeArgument _resultArgument;
    private static readonly MruCache<MethodInfo, Func<object, object[], object>> funcCache =
        new(MethodCallExpressionHelper.FuncCacheCapacity);
    private static readonly ReaderWriterLockSlim locker = new();

    public Collection<Type> GenericTypeArguments
    {
        get
        {
            _genericTypeArguments ??= new ValidatingCollection<Type>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _genericTypeArguments;
        }
    }

    public string MethodName { get; set; }

    public Collection<Argument> Parameters
    {
        get
        {
            _parameters ??= new ValidatingCollection<Argument>
            {
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                }
            };
            return _parameters;
        }
    }

    [DefaultValue(null)]
    public InArgument TargetObject { get; set; }

    [DefaultValue(null)]
    public Type TargetType { get; set; }

    [DefaultValue(false)]
    public bool RunAsynchronously { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        Collection<RuntimeArgument> arguments = new();

        Type targetObjectType = TypeHelper.ObjectType;

        if (TargetObject != null)
        {
            targetObjectType = TargetObject.ArgumentType;
        }

        RuntimeArgument targetObjectArgument = new("TargetObject", targetObjectType, ArgumentDirection.In);
        metadata.Bind(TargetObject, targetObjectArgument);
        arguments.Add(targetObjectArgument);

        _resultArgument = new RuntimeArgument("Result", typeof(TResult), ArgumentDirection.Out);
        metadata.Bind(Result, _resultArgument);
        arguments.Add(_resultArgument);

        // Parameters are named according to MethodInfo name if DetermineMethodInfo 
        // succeeds, otherwise arbitrary names are used.
        _methodResolver = CreateMethodResolver();

        _methodResolver.DetermineMethodInfo(metadata, funcCache, locker, ref _methodExecutor);
        _methodResolver.RegisterParameters(arguments);

        metadata.SetArgumentsCollection(arguments);

        _methodResolver.Trace();

        _methodExecutor?.Trace(this);
    }

    protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        => _methodExecutor.BeginExecuteMethod(context, callback, state);

    protected override TResult EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
    {
        _methodExecutor.EndExecuteMethod(context, result);
        return Result.Get(context);
    }

    private MethodResolver CreateMethodResolver()
    {
        return new MethodResolver
        {
            MethodName = MethodName,
            RunAsynchronously = RunAsynchronously,
            TargetType = TargetType,
            TargetObject = TargetObject,
            GenericTypeArguments = GenericTypeArguments,
            Parameters = Parameters,
            Result = _resultArgument,
            ResultType = typeof(TResult),
            Parent = this
        };
    }
}
