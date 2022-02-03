// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Expressions;
using System.Activities.Internals;
using System.Activities.Runtime;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Xaml;
using Microsoft.VisualBasic.Activities;

namespace System.Activities.XamlIntegration;

public sealed class ActivityWithResultConverter : TypeConverterBase
{
    public ActivityWithResultConverter()
        : base(typeof(Activity<>), typeof(ExpressionConverterHelper<>)) { }

    public ActivityWithResultConverter(Type type)
        : base(type, typeof(Activity<>), typeof(ExpressionConverterHelper<>)) { }

    internal static object GetRootTemplatedActivity(IServiceProvider serviceProvider)
    {
        // For now, we only support references to the root Activity when we're inside an Activity.Body
        // Note that in the case of nested activity bodies, this gives us the outer activity
        if (!(serviceProvider.GetService(typeof(IRootObjectProvider)) is IRootObjectProvider rootProvider))
        {
            return null;
        }

        if (!(serviceProvider.GetService(typeof(IAmbientProvider)) is IAmbientProvider ambientProvider))
        {
            return null;
        }

        if (!(serviceProvider.GetService(typeof(IXamlSchemaContextProvider)) is IXamlSchemaContextProvider
                schemaContextProvider))
        {
            return null;
        }

        var activityBody = GetXamlMember(schemaContextProvider.SchemaContext, typeof(Activity), "Implementation");
        var dynamicActivityBody =
            GetXamlMember(schemaContextProvider.SchemaContext, typeof(DynamicActivity), "Implementation");
        if (activityBody == null || dynamicActivityBody == null)
        {
            return null;
        }

        if (ambientProvider.GetFirstAmbientValue(null, activityBody, dynamicActivityBody) == null)
        {
            return null;
        }

        object rootActivity = rootProvider.RootObject as Activity;
        return rootActivity;
    }

    private static XamlMember GetXamlMember(XamlSchemaContext schemaContext, Type type, string memberName)
    {
        var xamlType = schemaContext.GetXamlType(type);
        if (xamlType == null)
        {
            return null;
        }

        var xamlMember = xamlType.GetMember(memberName);
        return xamlMember;
    }

    internal sealed class ExpressionConverterHelper<T> : TypeConverterHelper<Activity<T>>
    {
        private static readonly Regex s_literalEscapeRegex = new(@"^(%+\[)");
        private static readonly Type s_locationHelperType = typeof(LocationHelper<>);
        private readonly LocationHelper _locationHelper; // true if we're dealing with a Location
        private readonly Type _valueType;
        private TypeConverter _baseConverter;

        public ExpressionConverterHelper()
            : this(TypeHelper.AreTypesCompatible(typeof(T), typeof(Location))) { }

        public ExpressionConverterHelper(bool isLocationType)
        {
            _valueType = typeof(T);

            if (isLocationType)
            {
                Fx.Assert(_valueType.IsGenericType && _valueType.GetGenericArguments().Length == 1,
                    "Should only get Location<T> here");
                _valueType = _valueType.GetGenericArguments()[0];
                var concreteHelperType = s_locationHelperType.MakeGenericType(typeof(T), _valueType);
                _locationHelper = (LocationHelper) Activator.CreateInstance(concreteHelperType);
            }
        }

        private TypeConverter BaseConverter
        {
            get
            {
                _baseConverter ??= TypeDescriptor.GetConverter(_valueType);
                return _baseConverter;
            }
        }

        public override Activity<T> ConvertFromString(string text, ITypeDescriptorContext context)
        {
            if (IsExpression(text))
            {
                // Expression.  Use the expression parser.
                var expressionText = text.Substring(1, text.Length - 2);

                if (_locationHelper != null)
                    // TODO, need to decouple VisualBasicReference from this typeConverter
                {
                    return (Activity<T>) _locationHelper.CreateExpression(expressionText);
                }

                return new VisualBasicValue<T>
                {
                    ExpressionText = expressionText
                };
            }

            if (_locationHelper != null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidLocationExpression));
            }

            // look for "%[....]" escape pattern
            if (text.EndsWith("]", StringComparison.Ordinal) && s_literalEscapeRegex.IsMatch(text))
                // strip off the very front-most '%' from the original string
            {
                text = text.Substring(1, text.Length - 1);
            }

            T literalValue = text switch
            {
                T => (T) (object) text,
                // workaround for System.Runtime.Xaml bug
                "" => default,
                _  => (T) BaseConverter.ConvertFromString(context, text)
            };

            return new Literal<T> {Value = literalValue};
        }

        private static bool IsExpression(string text) => 
            text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal);

        // to perform the generics dance around Locations we need these helpers
        private abstract class LocationHelper
        {
            public abstract Activity CreateExpression(string expressionText);
        }

        private class LocationHelper<TLocationValue> : LocationHelper
        {
            public override Activity CreateExpression(string expressionText)
            {
                return new VisualBasicReference<TLocationValue>
                {
                    ExpressionText = expressionText
                };
            }
        }
    }
}
