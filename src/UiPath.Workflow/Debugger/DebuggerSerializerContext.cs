using System.ComponentModel;
using System.Windows.Markup;
using System.Xaml;

namespace System.Activities.Debugger
{
    /// <summary>
    /// Based on System.Xaml.XamlObjectReader.SerializerContext
    /// + System.Xaml.XamlObjectReader.TypeDescriptorAndValueSerializerContext
    /// + MS.Internal.Xaml.Runtime.ClrObjectRuntime
    /// </summary>
    internal class DebuggerSerializerContext : IValueSerializerContext, IXamlSchemaContextProvider
    {
        public DebuggerSerializerContext(XamlSchemaContext schemaContext)
        {
            SchemaContext = schemaContext;
        }

        public XamlSchemaContext SchemaContext { get; }

        public IContainer Container
        {
            get { return null; }
        }

        public object Instance { get; set; }

        public PropertyDescriptor PropertyDescriptor
        {
            get { return null; }
        }

        public object GetService(Type serviceType)
        {
            if (GetType().IsAssignableTo(serviceType))
            {
                return this;
            }

            return null;
        }

        public void OnComponentChanged()
        {
        }

        public bool OnComponentChanging()
        {
            return false;
        }

        public ValueSerializer GetValueSerializerFor(PropertyDescriptor propertyDescriptor)
        {
            return ValueSerializer.GetSerializerFor(propertyDescriptor);
        }

        public ValueSerializer GetValueSerializerFor(Type type)
        {
            return ValueSerializer.GetSerializerFor(type);
        }
        
        public bool CanSerializeToString(ValueSerializer valueSerializer, TypeConverter propertyConverter, TypeConverter actualTypeConverter, object propertyValue)
        {
            if (propertyValue == null || propertyConverter == null)  // redundant
            {
                return false;
            }

            if (propertyValue is string)  // redundant
            {
                return true;
            }

            if (valueSerializer == null || !valueSerializer.CanConvertToString(propertyValue, this))
            {
                return false;
            }

            if (propertyConverter.CanConvertFrom(this, typeof(string)))
            {
                return true;
            }

            return actualTypeConverter != propertyConverter &&
                   actualTypeConverter != null &&
                   actualTypeConverter.CanConvertFrom(this, typeof(string));
        }

        public bool CanConvertToMarkupExtension(TypeConverter propertyConverter, object propertyValue)
        {
            return propertyValue != null &&  // redundant
                   propertyConverter != null &&  // redundant
                   propertyConverter.CanConvertTo(this, typeof(MarkupExtension));
        }

        public bool CanConvertToString(TypeConverter converter, object propertyValue)
        {
            if (propertyValue == null || // redundant
                converter == null ||  // redundant
                converter is ReferenceConverter)
            {
                return false;
            }

            if (propertyValue is string) // redundant
            {
                return true;
            }

            return converter.CanConvertFrom(this, typeof(string)) &&
                   converter.CanConvertTo(this, typeof(string));
        }
    }
}
