// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace System.Activities.XamlIntegration;

public sealed class InOutArgumentConverter : TypeConverterBase
{
    public InOutArgumentConverter()
        : base(typeof(InOutArgument<>), typeof(InOutArgumentConverterHelper<>)) { }

    public InOutArgumentConverter(Type type)
        : base(type, typeof(InOutArgument<>), typeof(InOutArgumentConverterHelper<>)) { }

    internal sealed class InOutArgumentConverterHelper<T> : TypeConverterHelper<InOutArgument<T>>
    {
        private readonly ActivityWithResultConverter.ExpressionConverterHelper<Location<T>> _expressionHelper;

        public InOutArgumentConverterHelper()
        {
            _expressionHelper = new ActivityWithResultConverter.ExpressionConverterHelper<Location<T>>(true);
        }

        public override InOutArgument<T> ConvertFromString(string text, ITypeDescriptorContext context)
        {
            return new InOutArgument<T>
            {
                Expression = _expressionHelper.ConvertFromString(text.Trim(), context)
            };
        }
    }
}
