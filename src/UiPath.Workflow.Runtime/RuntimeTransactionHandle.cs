// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Transactions;

namespace System.Activities;
using Internals;
using Runtime;

[Fx.Tag.XamlVisible(false)]
[DataContract]
public sealed class RuntimeTransactionHandle : Handle, IExecutionProperty, IPropertyRegistrationCallback
{
    private ActivityExecutor _executor;
    private bool _isHandleInitialized;
    private bool _doNotAbort;
    private bool _isPropertyRegistered;
    private bool _isSuppressed;
    private TransactionScope _scope;
    private readonly Transaction _rootTransaction;

    public RuntimeTransactionHandle() { }

    // This ctor is used when we want to make a transaction ambient
    // without enlisting.  This is desirable for scenarios like WorkflowInvoker
    public RuntimeTransactionHandle(Transaction rootTransaction)
    {
        _rootTransaction = rootTransaction ?? throw FxTrace.Exception.ArgumentNull(nameof(rootTransaction));
        AbortInstanceOnTransactionFailure = false;
    }

    public bool AbortInstanceOnTransactionFailure
    {
        get => !_doNotAbort;
        set
        {
            ThrowIfRegistered(SR.CannotChangeAbortInstanceFlagAfterPropertyRegistration);
            _doNotAbort = !value;
        }
    }

    public bool SuppressTransaction
    {
        get => _isSuppressed;
        set
        {
            ThrowIfRegistered(SR.CannotSuppressAlreadyRegisteredHandle);
            _isSuppressed = value;
        }
    }

    [DataMember(Name = "executor")]
    internal ActivityExecutor SerializedExecutor
    {
        get => _executor;
        set => _executor = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "isHandleInitialized")]
    internal bool SerializedIsHandleInitialized
    {
        get => _isHandleInitialized;
        set => _isHandleInitialized = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "doNotAbort")]
    internal bool SerializedDoNotAbort
    {
        get => _doNotAbort;
        set => _doNotAbort = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "isPropertyRegistered")]
    internal bool SerializedIsPropertyRegistered
    {
        get => _isPropertyRegistered;
        set => _isPropertyRegistered = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "isSuppressed")]
    internal bool SerializedIsSuppressed
    {
        get => _isSuppressed;
        set => _isSuppressed = value;
    }

    internal bool IsRuntimeOwnedTransaction => _rootTransaction != null;

    private void ThrowIfRegistered(string message)
    {
        if (_isPropertyRegistered)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(message));
        }
    }

    private void ThrowIfNotRegistered(string message)
    {
        if (!_isPropertyRegistered)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(message));
        }
    }

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "This method is designed to be called from activities with handle access.")]
    public Transaction GetCurrentTransaction(NativeActivityContext context) => GetCurrentTransactionCore(context);

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "This method is designed to be called from activities with handle access.")]
    public Transaction GetCurrentTransaction(CodeActivityContext context) => GetCurrentTransactionCore(context);

    //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
    //    Justification = "This method is designed to be called from activities with handle access.")]
    public Transaction GetCurrentTransaction(AsyncCodeActivityContext context) => GetCurrentTransactionCore(context);

    private Transaction GetCurrentTransactionCore(ActivityContext context)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        context.ThrowIfDisposed();

        //If the transaction is a runtime transaction (i.e. an Invoke with ambient transaction case), then 
        //we do not require that it be registered since the handle created for the root transaction is never registered.
        if (_rootTransaction == null)
        {
            ThrowIfNotRegistered(SR.RuntimeTransactionHandleNotRegisteredAsExecutionProperty("GetCurrentTransaction"));
        }

        if (!_isHandleInitialized)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnInitializedRuntimeTransactionHandle));
        }

        if (SuppressTransaction)
        {
            return null;
        }

        return _executor.CurrentTransaction;
    }

    protected override void OnInitialize(HandleInitializationContext context)
    {
        _executor = context.Executor;
        _isHandleInitialized = true;

        if (_rootTransaction != null)
        {
            Fx.Assert(Owner == null, "rootTransaction should only be set at the root");
            _executor.SetTransaction(this, _rootTransaction, null, null);
        }

        base.OnInitialize(context);
    }

    protected override void OnUninitialize(HandleInitializationContext context)
    {
        if (_rootTransaction != null)
        {
            // If we have a host transaction we're responsible for exiting no persist
            _executor.ExitNoPersist();
        }

        _isHandleInitialized = false;
        base.OnUninitialize(context);
    }

    public void RequestTransactionContext(NativeActivityContext context, Action<NativeActivityTransactionContext, object> callback, object state)
        => RequestOrRequireTransactionContextCore(context, callback, state, false);

    public void RequireTransactionContext(NativeActivityContext context, Action<NativeActivityTransactionContext, object> callback, object state)
        => RequestOrRequireTransactionContextCore(context, callback, state, true);

    private void RequestOrRequireTransactionContextCore(NativeActivityContext context, Action<NativeActivityTransactionContext, object> callback, object state, bool isRequires)
    {
        if (context == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(context));
        }

        context.ThrowIfDisposed();

        if (context.HasRuntimeTransaction)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeTransactionAlreadyExists));
        }

        if (context.IsInNoPersistScope)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotSetRuntimeTransactionInNoPersist));
        }

        if (!_isHandleInitialized)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnInitializedRuntimeTransactionHandle));
        }

        if (SuppressTransaction)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeTransactionIsSuppressed));
        }

        if (isRequires)
        {
            if (context.RequiresTransactionContextWaiterExists)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.OnlyOneRequireTransactionContextAllowed));
            }

            ThrowIfNotRegistered(SR.RuntimeTransactionHandleNotRegisteredAsExecutionProperty("RequireTransactionContext"));
        }
        else
        {
            ThrowIfNotRegistered(SR.RuntimeTransactionHandleNotRegisteredAsExecutionProperty("RequestTransactionContext"));
        }

        context.RequestTransactionContext(isRequires, this, callback, state);
    }

    public void CompleteTransaction(NativeActivityContext context) => CompleteTransactionCore(context, null);

    public void CompleteTransaction(NativeActivityContext context, BookmarkCallback callback)
    {
        if (callback == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(callback));
        }

        CompleteTransactionCore(context, callback);
    }

    private void CompleteTransactionCore(NativeActivityContext context, BookmarkCallback callback)
    {
        context.ThrowIfDisposed();

        if (_rootTransaction != null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotCompleteRuntimeOwnedTransaction));
        }

        if (!context.HasRuntimeTransaction)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.NoRuntimeTransactionExists));
        }

        if (!_isHandleInitialized)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnInitializedRuntimeTransactionHandle));
        }

        if (SuppressTransaction)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeTransactionIsSuppressed));
        }

        context.CompleteTransaction(this, callback);
    }

    [Fx.Tag.Throws(typeof(TransactionException), "The transaction for this property is in a state incompatible with TransactionScope.")]
    void IExecutionProperty.SetupWorkflowThread()
    {
        if (SuppressTransaction)
        {
            _scope = new TransactionScope(TransactionScopeOption.Suppress);
            return;
        }

        if ((_executor != null) && _executor.HasRuntimeTransaction)
        {
            _scope = TransactionHelper.CreateTransactionScope(_executor.CurrentTransaction);
        }
    }

    void IExecutionProperty.CleanupWorkflowThread() => TransactionHelper.CompleteTransactionScope(ref _scope);

    void IPropertyRegistrationCallback.Register(RegistrationContext context)
    {
        if (!_isHandleInitialized)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnInitializedRuntimeTransactionHandle));
        }

        RuntimeTransactionHandle handle = (RuntimeTransactionHandle)context.FindProperty(typeof(RuntimeTransactionHandle).FullName);
        if (handle != null)
        {
            if (handle.SuppressTransaction)
            {
                _isSuppressed = true;
            }
        }

        _isPropertyRegistered = true;
    }

    void IPropertyRegistrationCallback.Unregister(RegistrationContext context) => _isPropertyRegistered = false;
}
