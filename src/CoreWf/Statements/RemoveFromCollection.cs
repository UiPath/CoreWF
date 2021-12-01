// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Statements;

//[SuppressMessage(FxCop.Category.Naming, FxCop.Rule.IdentifiersShouldNotHaveIncorrectSuffix, Justification = "Optimizing for XAML naming.")]
[ContentProperty("Collection")]
public sealed class RemoveFromCollection<T> : CodeActivity<bool>
{
    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<ICollection<T>> Collection { get; set; }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<T> Item { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        RuntimeArgument collectionArgument = new("Collection", typeof(ICollection<T>), ArgumentDirection.In, true);
        metadata.Bind(Collection, collectionArgument);

        RuntimeArgument itemArgument = new("Item", typeof(T), ArgumentDirection.In, true);
        metadata.Bind(Item, itemArgument);

        RuntimeArgument resultArgument = new("Result", typeof(bool), ArgumentDirection.Out);
        metadata.Bind(Result, resultArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                collectionArgument,
                itemArgument,
                resultArgument
            });
    }

    protected override bool Execute(CodeActivityContext context)
    {
        ICollection<T> collection = Collection.Get(context);
        if (collection == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CollectionActivityRequiresCollection(DisplayName)));
        }
        T item = Item.Get(context);

        return collection.Remove(item);
    }
}
