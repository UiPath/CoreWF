// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.ComponentModel;
    using Portable.Xaml.Markup;
    using System.Activities.Internals;

    [MarkupExtensionReturnType(typeof(object))]
    public sealed class PropertyReferenceExtension<T> : MarkupExtension
    {
        public PropertyReferenceExtension()
            : base()
        {
        }

        public string PropertyName
        {
            get;
            set;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (!string.IsNullOrEmpty(this.PropertyName))
            {
                object targetObject = ActivityWithResultConverter.GetRootTemplatedActivity(serviceProvider);
                if (targetObject != null)
                {
                    PropertyDescriptor property = TypeDescriptor.GetProperties(targetObject)[PropertyName];

                    if (property != null)
                    {
                        return property.GetValue(targetObject);
                    }
                }
            }

            throw FxTrace.Exception.AsError(
                new InvalidOperationException(SR.PropertyReferenceNotFound(this.PropertyName)));
        }
    }
}
