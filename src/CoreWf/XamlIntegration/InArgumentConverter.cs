// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.XamlIntegration
{
    using System;
    using System.ComponentModel;

    public sealed class InArgumentConverter : TypeConverterBase
    {
        public InArgumentConverter()
            : base(typeof(InArgument<>), typeof(InArgumentConverterHelper<>))
        {
        }

        public InArgumentConverter(Type type)
            : base(type, typeof(InArgument<>), typeof(InArgumentConverterHelper<>))
        {
        }

        internal sealed class InArgumentConverterHelper<T> : TypeConverterHelper<InArgument<T>>
        {
            private readonly ActivityWithResultConverter.ExpressionConverterHelper<T> valueExpressionHelper;

            public InArgumentConverterHelper()
            {
                this.valueExpressionHelper = new ActivityWithResultConverter.ExpressionConverterHelper<T>(false);
            }

            public override InArgument<T> ConvertFromString(string text, ITypeDescriptorContext context)
            {
                return new InArgument<T>
                    {
                        Expression = this.valueExpressionHelper.ConvertFromString(text, context)
                    };
            }
        }
    }
}
