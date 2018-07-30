// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Statements
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Portable.Xaml.Markup;
    using System.Collections.ObjectModel;
    using CoreWf.Internals;

    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotHaveIncorrectSuffix, Justification = "Optimizing for XAML naming.")]
    [ContentProperty("Collection")]
    public sealed class ClearCollection<T> : CodeActivity
    {
        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<ICollection<T>> Collection
        {
            get;
            set;
        }


        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument collectionArgument = new RuntimeArgument("Collection", typeof(ICollection<T>), ArgumentDirection.In, true);
            metadata.Bind(this.Collection, collectionArgument);

            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { collectionArgument });
        }

        protected override void Execute(CodeActivityContext context)
        {
            ICollection<T> collection = this.Collection.Get(context);
            if (collection == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CollectionActivityRequiresCollection(this.DisplayName)));
            }
            collection.Clear();
        }
    }
}
