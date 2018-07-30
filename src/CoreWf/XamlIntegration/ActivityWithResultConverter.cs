// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.XamlIntegration
{
    using System;
    using CoreWf.Expressions;
    using System.ComponentModel;
    using System.Text.RegularExpressions;
    using Portable.Xaml;
    using CoreWf.Runtime;
    using CoreWf.Internals;

#if NET45
    using Microsoft.VisualBasic.Activities;
    using Microsoft.VisualBasic.Activities.XamlIntegration; 
#endif

    public sealed class ActivityWithResultConverter : TypeConverterBase
    {
        public ActivityWithResultConverter()
            : base(typeof(Activity<>), typeof(ExpressionConverterHelper<>))
        {
        }

        public ActivityWithResultConverter(Type type)
            : base(type, typeof(Activity<>), typeof(ExpressionConverterHelper<>))
        {
        }

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
            if (!(serviceProvider.GetService(typeof(IXamlSchemaContextProvider)) is IXamlSchemaContextProvider schemaContextProvider))
            {
                return null;
            }
            XamlMember activityBody = GetXamlMember(schemaContextProvider.SchemaContext, typeof(Activity), "Implementation");
            XamlMember dynamicActivityBody = GetXamlMember(schemaContextProvider.SchemaContext, typeof(DynamicActivity), "Implementation");
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
            XamlType xamlType = schemaContext.GetXamlType(type);
            if (xamlType == null)
            {
                return null;
            }
            XamlMember xamlMember = xamlType.GetMember(memberName);
            return xamlMember;
        }

        internal sealed class ExpressionConverterHelper<T> : TypeConverterHelper<Activity<T>>
        {
            private static Regex LiteralEscapeRegex = new Regex(@"^(%+\[)");
            private static Type LocationHelperType = typeof(LocationHelper<>);
            private TypeConverter baseConverter;
            private readonly Type valueType;
            private readonly LocationHelper locationHelper; // true if we're dealing with a Location

            public ExpressionConverterHelper()
                : this(TypeHelper.AreTypesCompatible(typeof(T), typeof(Location)))
            {
            }

            public ExpressionConverterHelper(bool isLocationType)
            {
                this.valueType = typeof(T);

                if (isLocationType)
                {
                    Fx.Assert(this.valueType.IsGenericType && this.valueType.GetGenericArguments().Length == 1, "Should only get Location<T> here");
                    this.valueType = this.valueType.GetGenericArguments()[0];
                    Type concreteHelperType = LocationHelperType.MakeGenericType(typeof(T), this.valueType);
                    this.locationHelper = (LocationHelper)Activator.CreateInstance(concreteHelperType);
                }
            }

            private TypeConverter BaseConverter
            {
                get
                {
                    if (this.baseConverter == null)
                    {
                        this.baseConverter = TypeDescriptor.GetConverter(this.valueType);
                    }

                    return this.baseConverter;
                }
            }

            public override Activity<T> ConvertFromString(string text, ITypeDescriptorContext context)
            {
#if NET45
                if (IsExpression(text))
                {
                    // Expression.  Use the expression parser.
                    string expressionText = text.Substring(1, text.Length - 2);

                    if (this.locationHelper != null)
                    {
                        // TODO, 77787, need to decouple VisualBasicReference from this typeConverter
                        return (Activity<T>)this.locationHelper.CreateExpression(expressionText);
                    }
                    else
                    {
                        // TODO, 77787, need to decouple VisualBasicValue from this typeConverter
                        return new VisualBasicValue<T>()
                        {
                            ExpressionText = expressionText
                        };
                    }
                }
                else
                { 
#endif
                    if (this.locationHelper != null)
                    {
                        throw FxTrace.Exception.AsError(new InvalidOperationException(SR.InvalidLocationExpression));
                    }

                    // look for "%[....]" escape pattern
                    if (text.EndsWith("]", StringComparison.Ordinal) && LiteralEscapeRegex.IsMatch(text))
                    {
                        // strip off the very front-most '%' from the original string
                        text = text.Substring(1, text.Length - 1);
                    }

                    T literalValue;
                    if (text is T)
                    {
                        literalValue = (T)(object)text;
                    }
                    else if (text == string.Empty) // workaround for System.Runtime.Xaml bug
                    {
                        literalValue = default(T);
                    }
                    else
                    {
                        // Literal value.  Invoke the base type converter.
                        literalValue = (T)BaseConverter.ConvertFromString(context, text);
                    }

                    return new Literal<T> { Value = literalValue };
#if NET45
            } 
#endif
        }

            private static bool IsExpression(string text)
            {
                return (text.StartsWith("[", StringComparison.Ordinal) && text.EndsWith("]", StringComparison.Ordinal));
            }

            // to perform the generics dance around Locations we need these helpers
            private abstract class LocationHelper
            {
                public abstract Activity CreateExpression(string expressionText);
            }

            private class LocationHelper<TLocationValue> : LocationHelper
            {
                public override Activity CreateExpression(string expressionText)
                {
#if NET45
                    return new VisualBasicReference<TLocationValue>()
                    {
                        ExpressionText = expressionText
                    }; 
#else
                    return new Literal<TLocationValue>();
#endif
                }
            }
        } 
    }
}
