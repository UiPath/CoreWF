// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Internals;
using Runtime;

[DataContract]
public abstract class Handle
{
    private ActivityInstance _owner;

    // We check uninitialized because it should be false more often
    private bool _isUninitialized;

    protected Handle()
    {
        _isUninitialized = true;
    }

    public ActivityInstance Owner => _owner;

    public string ExecutionPropertyName => GetType().FullName;

    [DataMember(EmitDefaultValue = false, Name = "owner")]
    internal ActivityInstance SerializedOwner
    {
        get => _owner;
        set => _owner = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "isUninitialized")]
    internal bool SerializedIsUninitialized
    {
        get => _isUninitialized;
        set => _isUninitialized = value;
    }

    [DataMember(EmitDefaultValue = false)]
    internal bool CanBeRemovedWithExecutingChildren { get; set; }

    internal bool IsInitialized => !_isUninitialized;

    internal static string GetPropertyName(Type handleType)
    {
        Fx.Assert(TypeHelper.AreTypesCompatible(handleType, typeof(Handle)), "must pass in a Handle-based type here");
        return handleType.FullName;
    }

    internal void Initialize(HandleInitializationContext context)
    {
        _owner = context.OwningActivityInstance;
        _isUninitialized = false;

        OnInitialize(context);
    }

    internal void Reinitialize(ActivityInstance owner)
    {
        _owner = owner;
        _isUninitialized = false;
    }

    internal void Uninitialize(HandleInitializationContext context)
    {
        OnUninitialize(context);
        _isUninitialized = true;
    }

    protected virtual void OnInitialize(HandleInitializationContext context) { }

    protected virtual void OnUninitialize(HandleInitializationContext context) { }

    protected void ThrowIfUninitialized()
    {
        if (_isUninitialized)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.HandleNotInitialized));
        }
    }
}
