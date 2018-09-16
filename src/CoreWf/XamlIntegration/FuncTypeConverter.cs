// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.XamlIntegration
{
    using System;
    using Portable.Xaml;
    using CoreWf.Internals;

    public class FuncDeferringLoader : XamlDeferringLoader
    {
        public override object Load(XamlReader xamlReader, IServiceProvider context)
        {
            FuncFactory factory = FuncFactory.CreateFactory(xamlReader, context);
            factory.IgnoreParentSettings = true;
            return factory.GetFunc();
        }

        public override XamlReader Save(object value, IServiceProvider serviceProvider)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException(SR.SavingActivityToXamlNotSupported));
        }
    }
}


