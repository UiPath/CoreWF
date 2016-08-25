// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.CoreWf.Statements
{
    //[ContentProperty("Exception")]
    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotMatchKeywords, //Justification = "Optimizing for XAML naming. VB imperative users will [] qualify (e.g. New [Throw])")]
    public sealed class Throw : CodeActivity
    {
        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<Exception> Exception
        {
            get;
            set;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument exceptionArgument = new RuntimeArgument("Exception", typeof(Exception), ArgumentDirection.In, true);
            metadata.Bind(this.Exception, exceptionArgument);
            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { exceptionArgument });
        }

        protected override void Execute(CodeActivityContext context)
        {
            Exception exception = this.Exception.Get(context);

            if (exception == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Exception", this.GetType().Name, this.DisplayName)));
            }

            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(exception);
        }
    }
}
