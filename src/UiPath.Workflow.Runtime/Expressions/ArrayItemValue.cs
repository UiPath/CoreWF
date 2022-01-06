// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities.Expressions;

public sealed class ArrayItemValue<TItem> : CodeActivity<TItem>
{
    public ArrayItemValue()
        : base() { }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<TItem[]> Array { get; set; }

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<int> Index { get; set; }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        RuntimeArgument arrayArgument = new RuntimeArgument("Array", typeof(TItem[]), ArgumentDirection.In, true);
        metadata.Bind(Array, arrayArgument);

        RuntimeArgument indexArgument = new RuntimeArgument("Index", typeof(int), ArgumentDirection.In, true);
        metadata.Bind(Index, indexArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                arrayArgument,
                indexArgument,
            });
    }

    protected override TItem Execute(CodeActivityContext context)
    {
        TItem[] items = Array.Get(context);
        if (items == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Array", GetType().Name, DisplayName)));
        }

        int itemIndex = Index.Get(context);
        return items[itemIndex];
    }
}
