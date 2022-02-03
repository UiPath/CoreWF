// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Internals;
using System.ComponentModel;
using System.Windows.Markup;

namespace System.Activities.XamlIntegration;

[MarkupExtensionReturnType(typeof(object))]
public sealed class PropertyReferenceExtension<T> : MarkupExtension
{
    public string PropertyName { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (!string.IsNullOrEmpty(PropertyName))
        {
            var targetObject = ActivityWithResultConverter.GetRootTemplatedActivity(serviceProvider);
            if (targetObject != null)
            {
                var property = TypeDescriptor.GetProperties(targetObject)[PropertyName];

                if (property != null)
                {
                    return property.GetValue(targetObject);
                }
            }
        }

        throw FxTrace.Exception.AsError(
            new InvalidOperationException(SR.PropertyReferenceNotFound(PropertyName)));
    }
}
