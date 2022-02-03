// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace System.Activities.XamlIntegration;

public sealed class OutArgumentConverter : TypeConverterBase
{
    public OutArgumentConverter()
        : base(typeof(OutArgument<>), typeof(OutArgumentConverterHelper<>)) { }

    public OutArgumentConverter(Type type)
        : base(type, typeof(OutArgument<>), typeof(OutArgumentConverterHelper<>)) { }

    internal sealed class OutArgumentConverterHelper<T> : TypeConverterHelper<OutArgument<T>>
    {
        private readonly ActivityWithResultConverter.ExpressionConverterHelper<Location<T>> _expressionHelper;

        public OutArgumentConverterHelper()
        {
            _expressionHelper = new ActivityWithResultConverter.ExpressionConverterHelper<Location<T>>(true);
        }

        public override OutArgument<T> ConvertFromString(string text, ITypeDescriptorContext context)
        {
            return new OutArgument<T>
            {
                Expression = _expressionHelper.ConvertFromString(text.Trim(), context)
            };
        }
    }
}
