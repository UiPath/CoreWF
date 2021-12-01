// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace System.Activities.Expressions;

[ContentProperty("Indices")]
public sealed class MultidimensionalArrayItemReference<TItem> : CodeActivity<Location<TItem>>
{
    private Collection<InArgument<int>> _indices;

    [RequiredArgument]
    [DefaultValue(null)]
    public InArgument<Array> Array { get; set; }

    [DefaultValue(null)]
    public Collection<InArgument<int>> Indices
    {
        get
        {
            _indices ??= new ValidatingCollection<InArgument<int>>
            {   
                // disallow null values
                OnAddValidationCallback = item =>
                {
                    if (item == null)
                    {
                        throw FxTrace.Exception.ArgumentNull(nameof(item));
                    }
                },
            };
            return _indices;
        }
    }

    protected override void CacheMetadata(CodeActivityMetadata metadata)
    {
        if (Indices.Count == 0)
        {
            metadata.AddValidationError(SR.IndicesAreNeeded(GetType().Name, DisplayName));
        }

        RuntimeArgument arrayArgument = new("Array", typeof(Array), ArgumentDirection.In, true);
        metadata.Bind(Array, arrayArgument);
        metadata.AddArgument(arrayArgument);

        for (int i = 0; i < Indices.Count; i++)
        {
            RuntimeArgument indexArgument = new("Index_" + i, typeof(int), ArgumentDirection.In, true);
            metadata.Bind(Indices[i], indexArgument);
            metadata.AddArgument(indexArgument);
        }

        RuntimeArgument resultArgument = new("Result", typeof(Location<TItem>), ArgumentDirection.Out);
        metadata.Bind(Result, resultArgument);
        metadata.AddArgument(resultArgument);
    }

    protected override Location<TItem> Execute(CodeActivityContext context)
    {
        Array items = Array.Get(context);
            
        if (items == null)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Array", GetType().Name, DisplayName)));
        }

        Type realItemType = items.GetType().GetElementType();
        if (!TypeHelper.AreTypesCompatible(typeof(TItem), realItemType))
        {
            throw FxTrace.Exception.AsError(new InvalidCastException(SR.IncompatibleTypeForMultidimensionalArrayItemReference(typeof(TItem).Name, realItemType.Name)));
        }
        int[] itemIndex = new int[Indices.Count];
        for (int i = 0; i < Indices.Count; i++)
        {
            itemIndex[i] = Indices[i].Get(context);
        }
        return new MultidimensionArrayLocation(items, itemIndex);
    }

    [DataContract]
    internal class MultidimensionArrayLocation : Location<TItem>
    {
        private Array _array;
        private int[] _indices;

        public MultidimensionArrayLocation(Array array, int[] indices)
            : base()
        {
            _array = array;
            _indices = indices;
        }

        public override TItem Value
        {
            get => (TItem)_array.GetValue(_indices);
            set => _array.SetValue(value, _indices);
        }

        [DataMember(Name = "array")]
        internal Array Serializedarray
        {
            get => _array;
            set => _array = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "indices")]
        internal int[] SerializedIndices
        {
            get => _indices;
            set => _indices = value;
        }
    }
}
