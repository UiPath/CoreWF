// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System.Activities.Internals;
    using Portable.Xaml.Markup;

    public class ArgumentValueSerializer : ValueSerializer
    {        
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            if (!(value is Argument argument))
            {
                return false;
            }
            if (ActivityBuilder.HasPropertyReferences(value))
            {
                // won't be able to attach the property references if we convert to string
                return false;
            }

            return argument.CanConvertToString(context);
        }

        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            if (!(value is Argument argument))
            {
                // expect CanConvertToString() always comes before ConvertToString()
                throw FxTrace.Exception.Argument(nameof(value), SR.CannotSerializeExpression(value.GetType()));
            }

            return argument.ConvertToString(context);
        }
    }
}
