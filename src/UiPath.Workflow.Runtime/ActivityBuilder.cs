// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Debugger;
using System.Activities.Runtime;
using System.Activities.Validation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Markup;
using System.Xaml;

namespace System.Activities;

[ContentProperty("Implementation")]
public sealed class ActivityBuilder : IDebuggableWorkflowTree
{
    // define attached properties that will identify PropertyReferenceExtension-based
    // object properties
    private static readonly AttachableMemberIdentifier s_propertyReferencePropertyId =
        new(typeof(ActivityBuilder), "PropertyReference");

    private static readonly AttachableMemberIdentifier s_propertyReferencesPropertyId =
        new(typeof(ActivityBuilder), "PropertyReferences");

    private KeyedCollection<string, DynamicActivityProperty> _properties;
    private Collection<Constraint> _constraints;
    private Collection<Attribute> _attributes;

    public string Name { get; set; }

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
    public KeyedCollection<string, DynamicActivityProperty> Properties
    {
        get
        {
            _properties ??= new ActivityPropertyCollection();
            return _properties;
        }
    }

    [DependsOn("Properties")]
    [Browsable(false)]
    public Collection<Constraint> Constraints
    {
        get
        {
            _constraints ??= new Collection<Constraint>();
            return _constraints;
        }
    }

    [TypeConverter(TypeConverters.ImplementationVersionConverter)]
    [DefaultValue(null)]
    [DependsOn("Name")]
    public Version ImplementationVersion { get; set; }

    [DefaultValue(null)]
    [Browsable(false)]
    [DependsOn("Constraints")]
    public Activity Implementation { get; set; }

    // Back-compat workaround: PropertyReference shipped in 4.0. PropertyReferences is new in 4.5.
    //
    // Requirements:
    // - Runtime compat: Get/SetPropertyReference needs to continue to work, both when set programatically
    //   and when loading a doc which contains only one PropertyReference on an object.
    // - Serialization compat: If only one PropertyReference was set, we shouldn't serialize PropertyReferences.
    //   (Only affects when ActivityBuilder is used directly with XamlServices, since ActivityXamlServices
    //   will convert ActivityPropertyReference to PropertyReferenceExtension.)
    // - Usability: To avoid the designer needing to support two separate access methods, we want
    //   the value from SetPropertyReference to also appear in the PropertyReferences collection.

    // <ActivityBuilder.PropertyReference>activity property name</ActivityBuilder.PropertyReference>
    public static ActivityPropertyReference GetPropertyReference(object target) => 
        GetPropertyReferenceCollection(target).SingleItem;

    // <ActivityBuilder.PropertyReference>activity property name</ActivityBuilder.PropertyReference>
    public static void SetPropertyReference(object target, ActivityPropertyReference value) => 
        GetPropertyReferenceCollection(target).SingleItem = value;

    public static IList<ActivityPropertyReference> GetPropertyReferences(object target) => 
        GetPropertyReferenceCollection(target);

    public static bool ShouldSerializePropertyReference(object target)
    {
        var propertyReferences = GetPropertyReferenceCollection(target);
        return propertyReferences.Count == 1 && propertyReferences.SingleItem != null;
    }

    public static bool ShouldSerializePropertyReferences(object target)
    {
        var propertyReferences = GetPropertyReferenceCollection(target);
        return propertyReferences.Count > 1 || propertyReferences.SingleItem == null;
    }

    internal static bool HasPropertyReferences(object target)
    {
        if (AttachablePropertyServices.TryGetProperty(target, s_propertyReferencesPropertyId,
                out PropertyReferenceCollection propertyReferences))
        {
            return propertyReferences.Count > 0;
        }

        return false;
    }

    private static PropertyReferenceCollection GetPropertyReferenceCollection(object target)
    {
        if (!AttachablePropertyServices.TryGetProperty(target, s_propertyReferencesPropertyId,
                out PropertyReferenceCollection propertyReferences))
        {
            propertyReferences = new PropertyReferenceCollection(target);
            AttachablePropertyServices.SetProperty(target, s_propertyReferencesPropertyId, propertyReferences);
        }

        return propertyReferences;
    }

    Activity IDebuggableWorkflowTree.GetWorkflowRoot()
    {
        return Implementation;
    }

    internal static KeyedCollection<string, DynamicActivityProperty> CreateActivityPropertyCollection() => 
        new ActivityPropertyCollection();

    private class ActivityPropertyCollection : KeyedCollection<string, DynamicActivityProperty>
    {
        protected override string GetKeyForItem(DynamicActivityProperty item) => item.Name;
    }

    // See back-compat requirements in comment above. Design is:
    // - First value added to collection when it is empty becomes the single PropertyReference value
    // - If the single value is removed, then PropertyReference AP is removed
    // - If PropertyReference AP is set to null, we remove the single value.
    // - If PropertyReference is set to non-null, we replace the existing single value if there
    //    is one, or else add the new value to the collection.
    private class PropertyReferenceCollection : Collection<ActivityPropertyReference>
    {
        private readonly WeakReference _targetObject;
        private int _singleItemIndex = -1;

        public PropertyReferenceCollection(object target)
        {
            _targetObject = new WeakReference(target);
        }

        public ActivityPropertyReference SingleItem
        {
            get => _singleItemIndex >= 0 ? this[_singleItemIndex] : null;
            set
            {
                if (_singleItemIndex >= 0)
                {
                    if (value != null)
                    {
                        SetItem(_singleItemIndex, value);
                    }
                    else
                    {
                        RemoveItem(_singleItemIndex);
                    }
                }
                else if (value != null)
                {
                    Add(value);
                    if (Count > 1)
                    {
                        _singleItemIndex = Count - 1;
                        UpdateAttachedProperty();
                    }
                }
            }
        }

        protected override void ClearItems()
        {
            _singleItemIndex = -1;
            UpdateAttachedProperty();
        }

        protected override void InsertItem(int index, ActivityPropertyReference item)
        {
            base.InsertItem(index, item);
            if (index <= _singleItemIndex)
            {
                _singleItemIndex++;
            }
            else if (Count == 1)
            {
                Fx.Assert(_singleItemIndex < 0, "How did we have an index if we were empty?");
                _singleItemIndex = 0;
                UpdateAttachedProperty();
            }
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
            if (index < _singleItemIndex)
            {
                _singleItemIndex--;
            }
            else if (index == _singleItemIndex)
            {
                _singleItemIndex = -1;
                UpdateAttachedProperty();
            }
        }

        protected override void SetItem(int index, ActivityPropertyReference item)
        {
            base.SetItem(index, item);
            if (index == _singleItemIndex)
            {
                UpdateAttachedProperty();
            }
        }

        private void UpdateAttachedProperty()
        {
            var target = _targetObject.Target;
            if (target != null)
            {
                if (_singleItemIndex >= 0)
                {
                    AttachablePropertyServices.SetProperty(target, s_propertyReferencePropertyId, this[_singleItemIndex]);
                }
                else
                {
                    AttachablePropertyServices.RemoveProperty(target, s_propertyReferencePropertyId);
                }
            }
        }
    }
}

[ContentProperty("Implementation")]
public sealed class ActivityBuilder<TResult> : IDebuggableWorkflowTree
{
    private KeyedCollection<string, DynamicActivityProperty> _properties;
    private Collection<Constraint> _constraints;
    private Collection<Attribute> _attributes;

    public string Name { get; set; }

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
    public KeyedCollection<string, DynamicActivityProperty> Properties
    {
        get
        {
            _properties ??= ActivityBuilder.CreateActivityPropertyCollection();
            return _properties;
        }
    }

    [DependsOn("Properties")]
    [Browsable(false)]
    public Collection<Constraint> Constraints
    {
        get
        {
            _constraints ??= new Collection<Constraint>();
            return _constraints;
        }
    }

    [TypeConverter(TypeConverters.ImplementationVersionConverter)]
    [DefaultValue(null)]
    [DependsOn("Name")]
    public Version ImplementationVersion { get; set; }

    [DefaultValue(null)]
    [Browsable(false)]
    [DependsOn("Constraints")]
    public Activity Implementation { get; set; }

    Activity IDebuggableWorkflowTree.GetWorkflowRoot()
    {
        return Implementation;
    }
}
