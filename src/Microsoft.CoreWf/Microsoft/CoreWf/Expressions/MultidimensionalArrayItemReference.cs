// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Runtime.Collections;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Expressions
{
    //[ContentProperty("Indices")]
    public sealed class MultidimensionalArrayItemReference<TItem> : CodeActivity<Location<TItem>>
    {
        private Collection<InArgument<int>> _indices;

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<Array> Array
        {
            get;
            set;
        }

        [DefaultValue(null)]
        public Collection<InArgument<int>> Indices
        {
            get
            {
                if (_indices == null)
                {
                    _indices = new ValidatingCollection<InArgument<int>>
                    {
                        // disallow null values
                        OnAddValidationCallback = item =>
                        {
                            if (item == null)
                            {
                                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
                            }
                        },
                    };
                }
                return _indices;
            }
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (this.Indices.Count == 0)
            {
                metadata.AddValidationError(SR.IndicesAreNeeded(this.GetType().Name, this.DisplayName));
            }

            RuntimeArgument arrayArgument = new RuntimeArgument("Array", typeof(Array), ArgumentDirection.In, true);
            metadata.Bind(this.Array, arrayArgument);
            metadata.AddArgument(arrayArgument);

            for (int i = 0; i < this.Indices.Count; i++)
            {
                RuntimeArgument indexArgument = new RuntimeArgument("Index_" + i, typeof(int), ArgumentDirection.In, true);
                metadata.Bind(this.Indices[i], indexArgument);
                metadata.AddArgument(indexArgument);
            }

            RuntimeArgument resultArgument = new RuntimeArgument("Result", typeof(Location<TItem>), ArgumentDirection.Out);
            metadata.Bind(this.Result, resultArgument);
            metadata.AddArgument(resultArgument);
        }

        protected override Location<TItem> Execute(CodeActivityContext context)
        {
            Array items = this.Array.Get(context);

            if (items == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Array", this.GetType().Name, this.DisplayName)));
            }

            Type realItemType = items.GetType().GetElementType();
            if (!TypeHelper.AreTypesCompatible(typeof(TItem), realItemType))
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidCastException(SR.IncompatibleTypeForMultidimensionalArrayItemReference(typeof(TItem).Name, realItemType.Name)));
            }
            int[] itemIndex = new int[this.Indices.Count];
            for (int i = 0; i < this.Indices.Count; i++)
            {
                itemIndex[i] = this.Indices[i].Get(context);
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
                get
                {
                    return (TItem)_array.GetValue(_indices);
                }
                set
                {
                    _array.SetValue(value, _indices);
                }
            }

            [DataMember(Name = "array")]
            internal Array Serializedarray
            {
                get { return _array; }
                set { _array = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "indices")]
            internal int[] SerializedIndices
            {
                get { return _indices; }
                set { _indices = value; }
            }
        }
    }
}
