// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace System.Activities.Expressions;

public sealed class ArrayItemReference<TItem> : CodeActivity<Location<TItem>>
{
    public ArrayItemReference()
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

        RuntimeArgument resultArgument = new RuntimeArgument("Result", typeof(Location<TItem>), ArgumentDirection.Out);
        metadata.Bind(Result, resultArgument);

        metadata.SetArgumentsCollection(
            new Collection<RuntimeArgument>
            {
                arrayArgument,
                indexArgument,
                resultArgument
            });
    }

    protected override Location<TItem> Execute(CodeActivityContext context)
    {
        TItem[] items = Array.Get(context);
        if (items == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Array", GetType().Name, DisplayName)));
        }
        int itemIndex = Index.Get(context);
        return new ArrayLocation(items, itemIndex);
    }

    [DataContract]
    internal class ArrayLocation : Location<TItem>
    {
        private TItem[] _array;
        private int _index;

        public ArrayLocation(TItem[] array, int index)
            : base()
        {
            _array = array;
            _index = index;
        }

        public override TItem Value
        {
            get => _array[_index];
            set => _array[_index] = value;
        }

        [DataMember(Name = "array")]
        internal TItem[] SerializedArray
        {
            get => _array;
            set => _array = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "index")]
        internal int SerializedIndex
        {
            get => _index;
            set => _index = value;
        }
    }
}
