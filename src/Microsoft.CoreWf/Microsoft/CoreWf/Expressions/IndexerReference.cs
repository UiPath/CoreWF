// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Runtime.Collections;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.CoreWf.Expressions
{
    //[ContentProperty("Indices")]
    public sealed class IndexerReference<TOperand, TItem> : CodeActivity<Location<TItem>>
    {
        private Collection<InArgument> _indices;
        private MethodInfo _getMethod;
        private MethodInfo _setMethod;
        private Func<object, object[], object> _getFunc;
        private Func<object, object[], object> _setFunc;

        private static MruCache<MethodInfo, Func<object, object[], object>> s_funcCache =
            new MruCache<MethodInfo, Func<object, object[], object>>(MethodCallExpressionHelper.FuncCacheCapacity);
        private static ReaderWriterLockSlim s_locker = new ReaderWriterLockSlim();

        [RequiredArgument]
        [DefaultValue(null)]
        public InArgument<TOperand> Operand
        {
            get;
            set;
        }

        [RequiredArgument]
        [DefaultValue(null)]
        public Collection<InArgument> Indices
        {
            get
            {
                if (_indices == null)
                {
                    _indices = new ValidatingCollection<InArgument>
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
            MethodInfo oldGetMethod = _getMethod;
            MethodInfo oldSetMethod = _setMethod;

            if (typeof(TOperand).GetTypeInfo().IsValueType)
            {
                metadata.AddValidationError(SR.TargetTypeIsValueType(this.GetType().Name, this.DisplayName));
            }
            if (this.Indices.Count == 0)
            {
                metadata.AddValidationError(SR.IndicesAreNeeded(this.GetType().Name, this.DisplayName));
            }
            else
            {
                IndexerHelper.CacheMethod<TOperand, TItem>(this.Indices, ref _getMethod, ref _setMethod);
                if (_setMethod == null)
                {
                    metadata.AddValidationError(SR.SpecialMethodNotFound("set_Item", typeof(TOperand).Name));
                }
            }

            RuntimeArgument operandArgument = new RuntimeArgument("Operand", typeof(TOperand), ArgumentDirection.In, true);
            metadata.Bind(this.Operand, operandArgument);
            metadata.AddArgument(operandArgument);

            IndexerHelper.OnGetArguments<TItem>(this.Indices, this.Result, metadata);
            if (MethodCallExpressionHelper.NeedRetrieve(_getMethod, oldGetMethod, _getFunc))
            {
                _getFunc = MethodCallExpressionHelper.GetFunc(metadata, _getMethod, s_funcCache, s_locker);
            }
            if (MethodCallExpressionHelper.NeedRetrieve(_setMethod, oldSetMethod, _setFunc))
            {
                _setFunc = MethodCallExpressionHelper.GetFunc(metadata, _setMethod, s_funcCache, s_locker);
            }
        }

        protected override Location<TItem> Execute(CodeActivityContext context)
        {
            object[] indicesValue = new object[this.Indices.Count];

            for (int i = 0; i < this.Indices.Count; i++)
            {
                indicesValue[i] = this.Indices[i].Get(context);
            }

            TOperand operandValue = this.Operand.Get(context);
            if (operandValue == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.MemberCannotBeNull("Operand", this.GetType().Name, this.DisplayName)));
            }

            return new IndexerLocation(operandValue, indicesValue, _getMethod, _setMethod, _getFunc, _setFunc);
        }


        [DataContract]
        internal class IndexerLocation : Location<TItem>
        {
            private TOperand _operand;

            private object[] _indices;

            private object[] _parameters;

            private MethodInfo _getMethod;

            private MethodInfo _setMethod;

            private Func<object, object[], object> _getFunc;
            private Func<object, object[], object> _setFunc;

            public IndexerLocation(TOperand operand, object[] indices, MethodInfo getMethod, MethodInfo setMethod,
                Func<object, object[], object> getFunc, Func<object, object[], object> setFunc)
                : base()
            {
                _operand = operand;
                _indices = indices;
                _getMethod = getMethod;
                _setMethod = setMethod;
                _getFunc = getFunc;
                _setFunc = setFunc;
            }

            public override TItem Value
            {
                get
                {
                    Fx.Assert(_operand != null, "operand must not be null");
                    Fx.Assert(_indices != null, "indices must not be null");
                    if (_getFunc != null)
                    {
                        return (TItem)_getFunc(_operand, _indices);
                    }
                    else if (_getMethod != null)
                    {
                        return (TItem)_getMethod.Invoke(_operand, _indices);
                    }
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.SpecialMethodNotFound("get_Item", typeof(TOperand).Name)));
                }
                set
                {
                    Fx.Assert(_setMethod != null, "setMethod must not be null");
                    Fx.Assert(_operand != null, "operand must not be null");
                    Fx.Assert(_indices != null, "indices must not be null");
                    if (_parameters == null)
                    {
                        _parameters = new object[_indices.Length + 1];
                        for (int i = 0; i < _indices.Length; i++)
                        {
                            _parameters[i] = _indices[i];
                        }
                        _parameters[_parameters.Length - 1] = value;
                    }
                    if (_setFunc != null)
                    {
                        _setFunc(_operand, _parameters);
                    }
                    else
                    {
                        _setMethod.Invoke(_operand, _parameters);
                    }
                }
            }

            [DataMember(EmitDefaultValue = false, Name = "operand")]
            internal TOperand SerializedOperand
            {
                get { return _operand; }
                set { _operand = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "indices")]
            internal object[] SerializedIndices
            {
                get { return _indices; }
                set { _indices = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "parameters")]
            internal object[] SerializedParameters
            {
                get { return _parameters; }
                set { _parameters = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "getMethod")]
            internal MethodInfo SerializedGetMethod
            {
                get { return _getMethod; }
                set { _getMethod = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "setMethod")]
            internal MethodInfo SerializedSetMethod
            {
                get { return _setMethod; }
                set { _setMethod = value; }
            }
        }
    }
}
