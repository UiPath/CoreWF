// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities;
using Runtime;
using Validation;

[ContentProperty("Implementation")]
public sealed class DynamicActivity : Activity, ICustomTypeDescriptor, IDynamicActivity
{
    private Activity _runtimeImplementation;
    private readonly DynamicActivityTypeDescriptor _typeDescriptor;
    private Collection<Attribute> _attributes;

    public DynamicActivity()
        : base()
    {
        _typeDescriptor = new DynamicActivityTypeDescriptor(this);
    }

    public string Name
    {
        get => _typeDescriptor.Name;
        set => _typeDescriptor.Name = value;
    }

    [DependsOn("Name")]
    public Collection<Attribute> Attributes
    {
        get
        {
            _attributes ??= new Collection<Attribute>();
            return _attributes;
        }
    }

    [Browsable(false)]
    [DependsOn("Attributes")]
    public KeyedCollection<string, DynamicActivityProperty> Properties => _typeDescriptor.Properties;

    [DependsOn("Properties")]
    public new Collection<Constraint> Constraints => base.Constraints;

    [TypeConverter(TypeConverters.ImplementationVersionConverter)]
    [DefaultValue(null)]
    public new Version ImplementationVersion
    {
        get => base.ImplementationVersion;
        set => base.ImplementationVersion = value;
    }

    [XamlDeferLoad(OtherXaml.FuncDeferringLoader, OtherXaml.Activity)]
    [DefaultValue(null)]
    [Browsable(false)]
    [Ambient]
    public new Func<Activity> Implementation
    {
        get => base.Implementation;
        set => base.Implementation = value;
    }

    KeyedCollection<string, DynamicActivityProperty> IDynamicActivity.Properties => Properties;

    internal override void InternalExecute(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        if (_runtimeImplementation != null)
        {
            executor.ScheduleActivity(_runtimeImplementation, instance, null, null, null);
        }
    }

    sealed internal override void OnInternalCacheMetadata(bool createEmptyBindings)
    {
        Activity body = null;
        if (Implementation != null)
        {
            body = Implementation();
        }

        if (body != null)
        {
            SetImplementationChildrenCollection(new Collection<Activity> { body });
        }

        // Always cache the last body that we returned
        _runtimeImplementation = body;

        ReflectedInformation information = new(this);

        SetImportedChildrenCollection(information.GetChildren());
        SetVariablesCollection(information.GetVariables());
        SetImportedDelegatesCollection(information.GetDelegates());
        SetArgumentsCollection(information.GetArguments(), createEmptyBindings);
    }

    AttributeCollection ICustomTypeDescriptor.GetAttributes() => _typeDescriptor.GetAttributes();

    string ICustomTypeDescriptor.GetClassName() => _typeDescriptor.GetClassName();

    string ICustomTypeDescriptor.GetComponentName() => _typeDescriptor.GetComponentName();

    TypeConverter ICustomTypeDescriptor.GetConverter() => _typeDescriptor.GetConverter();

    EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => _typeDescriptor.GetDefaultEvent();

    PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => _typeDescriptor.GetDefaultProperty();

    object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => _typeDescriptor.GetEditor(editorBaseType);

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => _typeDescriptor.GetEvents(attributes);

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => _typeDescriptor.GetEvents();

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => _typeDescriptor.GetProperties();

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => _typeDescriptor.GetProperties(attributes);

    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => _typeDescriptor.GetPropertyOwner(pd);
}

[ContentProperty("Implementation")]
public sealed class DynamicActivity<TResult> : Activity<TResult>, ICustomTypeDescriptor, IDynamicActivity
{
    private Activity _runtimeImplementation;
    private readonly DynamicActivityTypeDescriptor _typeDescriptor;
    private Collection<Attribute> _attributes;

    public DynamicActivity()
        : base()
    {
        _typeDescriptor = new DynamicActivityTypeDescriptor(this);
    }

    public string Name
    {
        get => _typeDescriptor.Name;
        set => _typeDescriptor.Name = value;
    }

    [DependsOn("Name")]
    public Collection<Attribute> Attributes
    {
        get
        {
            _attributes ??= new Collection<Attribute>();
            return _attributes;
        }
    }

    [Browsable(false)]
    [DependsOn("Attributes")]
    public KeyedCollection<string, DynamicActivityProperty> Properties => _typeDescriptor.Properties;

    [DependsOn("Properties")]
    public new Collection<Constraint> Constraints => base.Constraints;

    [TypeConverter(TypeConverters.ImplementationVersionConverter)]
    [DefaultValue(null)]
    public new Version ImplementationVersion
    {
        get => base.ImplementationVersion;
        set => base.ImplementationVersion = value;
    }

    [XamlDeferLoad(OtherXaml.FuncDeferringLoader, OtherXaml.Activity)]
    [DefaultValue(null)]
    [Browsable(false)]
    [Ambient]
    public new Func<Activity> Implementation
    {
        get => base.Implementation;
        set => base.Implementation = value;
    }

    KeyedCollection<string, DynamicActivityProperty> IDynamicActivity.Properties => Properties;

    internal override void InternalExecute(ActivityInstance instance, ActivityExecutor executor, BookmarkManager bookmarkManager)
    {
        if (_runtimeImplementation != null)
        {
            executor.ScheduleActivity(_runtimeImplementation, instance, null, null, null);
        }
    }

    sealed internal override void OnInternalCacheMetadataExceptResult(bool createEmptyBindings)
    {
        Activity body = null;
        if (Implementation != null)
        {
            body = Implementation();
        }

        if (body != null)
        {
            SetImplementationChildrenCollection(new Collection<Activity> { body });
        }

        // Always cache the last body that we returned
        _runtimeImplementation = body;

        ReflectedInformation information = new(this);

        SetImportedChildrenCollection(information.GetChildren());
        SetVariablesCollection(information.GetVariables());
        SetImportedDelegatesCollection(information.GetDelegates());
        SetArgumentsCollection(information.GetArguments(), createEmptyBindings);
    }

    AttributeCollection ICustomTypeDescriptor.GetAttributes() => _typeDescriptor.GetAttributes();

    string ICustomTypeDescriptor.GetClassName() => _typeDescriptor.GetClassName();

    string ICustomTypeDescriptor.GetComponentName() => _typeDescriptor.GetComponentName();

    TypeConverter ICustomTypeDescriptor.GetConverter() => _typeDescriptor.GetConverter();

    EventDescriptor ICustomTypeDescriptor.GetDefaultEvent() => _typeDescriptor.GetDefaultEvent();

    PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty() => _typeDescriptor.GetDefaultProperty();

    object ICustomTypeDescriptor.GetEditor(Type editorBaseType) => _typeDescriptor.GetEditor(editorBaseType);

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes) => _typeDescriptor.GetEvents(attributes);

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => _typeDescriptor.GetEvents();

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => _typeDescriptor.GetProperties();

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes) => _typeDescriptor.GetProperties(attributes);

    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd) => _typeDescriptor.GetPropertyOwner(pd);
}
