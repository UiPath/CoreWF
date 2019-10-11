// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities.Statements
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Markup;
    using System.Activities.Internals;

    //[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotHaveIncorrectSuffix, Justification = "Optimizing for XAML naming.")]
    [ContentProperty("Collection")]
    public sealed class ExistsInCollection<T> : CodeActivity<bool>
    {
        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<ICollection<T>> Collection
        {
            get;
            set;
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<T> Item
        {
            get;
            set;
        }

        //override to no-op because of performance
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            RuntimeArgument collectionArgument = new RuntimeArgument("Collection", typeof(ICollection<T>), ArgumentDirection.In, true);
            metadata.Bind(this.Collection, collectionArgument);

            RuntimeArgument itemArgument = new RuntimeArgument("Item", typeof(T), ArgumentDirection.In, true);
            metadata.Bind(this.Item, itemArgument);

            metadata.SetArgumentsCollection(
                new Collection<RuntimeArgument>
                {
                    collectionArgument,
                    itemArgument,
                });
        }

        protected override bool Execute(CodeActivityContext context)
        {
            ICollection<T> collection = this.Collection.Get(context);
            if (collection == null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CollectionActivityRequiresCollection(this.DisplayName)));
            }
            T item = this.Item.Get(context);
            
            return collection.Contains(item);
        }
    }
}
