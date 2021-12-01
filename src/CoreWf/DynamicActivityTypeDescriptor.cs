// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.ObjectModel;

namespace System.Activities;
using Internals;

internal class DynamicActivityTypeDescriptor : ICustomTypeDescriptor
{
    private PropertyDescriptorCollection _cachedProperties;
    private readonly Activity _owner;

    public DynamicActivityTypeDescriptor(Activity owner)
    {
        _owner = owner;
        Properties = new ActivityPropertyCollection(this);
    }

    public string Name { get; set; }

    public KeyedCollection<string, DynamicActivityProperty> Properties { get; private set; }

    public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(_owner, true);

    public string GetClassName()
    {
        if (Name != null)
        {
            return Name;
        }

        return TypeDescriptor.GetClassName(_owner, true);
    }

    public string GetComponentName() => TypeDescriptor.GetComponentName(_owner, true);

    public TypeConverter GetConverter() => TypeDescriptor.GetConverter(_owner, true);

    public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(_owner, true);

    public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(_owner, true);

    public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(_owner, editorBaseType, true);

    public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(_owner, attributes, true);

    public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(_owner, true);

    public PropertyDescriptorCollection GetProperties() => GetProperties(null);

    public PropertyDescriptorCollection GetProperties(Attribute[] attributes = null)
    {
        PropertyDescriptorCollection result = _cachedProperties;
        if (result != null)
        {
            return result;
        }

        PropertyDescriptorCollection dynamicProperties;
        if (attributes != null)
        {
            dynamicProperties = TypeDescriptor.GetProperties(_owner, attributes, true);
        }
        else
        {
            dynamicProperties = TypeDescriptor.GetProperties(_owner, true);
        }

        // initial capacity is Properties + Name + Body 
        List<PropertyDescriptor> propertyDescriptors = new(Properties.Count + 2);
        for (int i = 0; i < dynamicProperties.Count; i++)
        {
            PropertyDescriptor dynamicProperty = dynamicProperties[i];
            if (dynamicProperty.IsBrowsable)
            {
                propertyDescriptors.Add(dynamicProperty);
            }
        }

        foreach (DynamicActivityProperty property in Properties)
        {
            if (string.IsNullOrEmpty(property.Name))
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.ActivityPropertyRequiresName(_owner.DisplayName)));
            }
            if (property.Type == null)
            {
                throw FxTrace.Exception.AsError(new ValidationException(SR.ActivityPropertyRequiresType(_owner.DisplayName)));
            }
            propertyDescriptors.Add(new DynamicActivityPropertyDescriptor(property, _owner.GetType()));
        }

        result = new PropertyDescriptorCollection(propertyDescriptors.ToArray());
        _cachedProperties = result;
        return result;
    }

    public object GetPropertyOwner(PropertyDescriptor pd) => _owner;

    private class DynamicActivityPropertyDescriptor : PropertyDescriptor
    {
        private AttributeCollection _attributes;
        private readonly DynamicActivityProperty _activityProperty;
        private readonly Type _componentType;

        public DynamicActivityPropertyDescriptor(DynamicActivityProperty activityProperty, Type componentType)
            : base(activityProperty.Name, null)
        {
            _activityProperty = activityProperty;
            _componentType = componentType;
        }

        public override Type ComponentType => _componentType;

        public override AttributeCollection Attributes
        {
            get
            {
                if (_attributes == null)
                {
                    AttributeCollection inheritedAttributes = base.Attributes;
                    Collection<Attribute> propertyAttributes = _activityProperty.Attributes;
                    Attribute[] totalAttributes = new Attribute[inheritedAttributes.Count + propertyAttributes.Count + 1];
                    inheritedAttributes.CopyTo(totalAttributes, 0);
                    propertyAttributes.CopyTo(totalAttributes, inheritedAttributes.Count);
                    totalAttributes[inheritedAttributes.Count + propertyAttributes.Count] = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden);
                    _attributes = new AttributeCollection(totalAttributes);
                }
                return _attributes;
            }
        }

        public override bool IsReadOnly => false;

        public override Type PropertyType => _activityProperty.Type;

        public override object GetValue(object component)
        {
            if (component is not IDynamicActivity owner || !owner.Properties.Contains(_activityProperty))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidDynamicActivityProperty(Name)));
            }

            return _activityProperty.Value;
        }

        public override void SetValue(object component, object value)
        {
            if (component is not IDynamicActivity owner || !owner.Properties.Contains(_activityProperty))
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidDynamicActivityProperty(Name)));
            }

            _activityProperty.Value = value;
        }

        public override bool CanResetValue(object component) => false;

        public override void ResetValue(object component) { }

        public override bool ShouldSerializeValue(object component) => false;

        protected override void FillAttributes(IList attributeList)
        {
            if (attributeList == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(attributeList));
            }

            attributeList.Add(new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden));
        }
    }

    private class ActivityPropertyCollection : KeyedCollection<string, DynamicActivityProperty>
    {
        private readonly DynamicActivityTypeDescriptor _parent;

        public ActivityPropertyCollection(DynamicActivityTypeDescriptor parent)
            : base()
        {
            _parent = parent;
        }

        protected override void InsertItem(int index, DynamicActivityProperty item)
        {
            if (item == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(item));
            }

            if (Contains(item.Name))
            {
                throw FxTrace.Exception.AsError(new ArgumentException(SR.DynamicActivityDuplicatePropertyDetected(item.Name), nameof(item)));
            }

            InvalidateCache();
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, DynamicActivityProperty item)
        {
            if (item == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(item));
            }

            // We don't want self-assignment to throw. Note that if this[index] has the same
            // name as item, no other element in the collection can.
            if (!this[index].Name.Equals(item.Name) && Contains(item.Name))
            {
                throw FxTrace.Exception.AsError(new ArgumentException(SR.DynamicActivityDuplicatePropertyDetected(item.Name), nameof(item)));
            }

            InvalidateCache();
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            InvalidateCache();
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            InvalidateCache();
            base.ClearItems();
        }

        protected override string GetKeyForItem(DynamicActivityProperty item) => item.Name;

        private void InvalidateCache() => _parent._cachedProperties = null;
    }
}
