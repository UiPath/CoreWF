// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CoreWf.Runtime;
using CoreWf.Runtime.Collections;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

namespace CoreWf.Expressions
{
    //[ContentProperty("Indices")]
    public sealed class ValueTypeIndexerReference<TOperand, TItem> : CodeActivity<Location<TItem>>
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
        public InOutArgument<TOperand> OperandLocation
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
                                throw CoreWf.Internals.FxTrace.Exception.ArgumentNull("item");
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
            if (!typeof(TOperand).GetTypeInfo().IsValueType)
            {
                metadata.AddValidationError(SR.TypeMustbeValueType(typeof(TOperand).Name));
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

            RuntimeArgument operandArgument = new RuntimeArgument("OperandLocation", typeof(TOperand), ArgumentDirection.InOut, true);
            metadata.Bind(this.OperandLocation, operandArgument);
            metadata.AddArgument(operandArgument);

            IndexerHelper.OnGetArguments<TItem>(this.Indices, this.Result, metadata);

            if (MethodCallExpressionHelper.NeedRetrieve(_getMethod, oldGetMethod, _getFunc))
            {
                _getFunc = MethodCallExpressionHelper.GetFunc(metadata, _getMethod, s_funcCache, s_locker);
            }
            if (MethodCallExpressionHelper.NeedRetrieve(_setMethod, oldSetMethod, _setFunc))
            {
                _setFunc = MethodCallExpressionHelper.GetFunc(metadata, _setMethod, s_funcCache, s_locker, true);
            }
        }

        protected override Location<TItem> Execute(CodeActivityContext context)
        {
            object[] indicesValue = new object[this.Indices.Count];
            for (int i = 0; i < this.Indices.Count; i++)
            {
                indicesValue[i] = this.Indices[i].Get(context);
            }
            Location<TOperand> operandLocationValue = this.OperandLocation.GetLocation(context);
            Fx.Assert(operandLocationValue != null, "OperandLocation must not be null");
            return new IndexerLocation(operandLocationValue, indicesValue, _getMethod, _setMethod, _getFunc, _setFunc);
        }

        [DataContract]
        internal class IndexerLocation : Location<TItem>
        {
            private Location<TOperand> _operandLocation;

            private object[] _indices;

            private object[] _parameters;

            private MethodInfo _getMethod;

            private MethodInfo _setMethod;

            private Func<object, object[], object> _getFunc;
            private Func<object, object[], object> _setFunc;

            public IndexerLocation(Location<TOperand> operandLocation, object[] indices, MethodInfo getMethod, MethodInfo setMethod,
                Func<object, object[], object> getFunc, Func<object, object[], object> setFunc)
                : base()
            {
                _operandLocation = operandLocation;
                _indices = indices;
                _getMethod = getMethod;
                _setMethod = setMethod;
                _setFunc = setFunc;
                _getFunc = getFunc;
            }

            public override TItem Value
            {
                get
                {
                    Fx.Assert(_operandLocation != null, "operandLocation must not be null");
                    Fx.Assert(_indices != null, "indices must not be null");
                    if (_getFunc != null)
                    {
                        return (TItem)_getFunc(_operandLocation.Value, _indices);
                    }
                    else if (_getMethod != null)
                    {
                        return (TItem)_getMethod.Invoke(_operandLocation.Value, _indices);
                    }
                    throw CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.SpecialMethodNotFound("get_Item", typeof(TOperand).Name)));
                }
                set
                {
                    Fx.Assert(_setMethod != null, "setMethod must not be null");
                    Fx.Assert(_operandLocation != null, "operandLocation must not be null");
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
                    object copy = _operandLocation.Value;
                    if (_setFunc != null)
                    {
                        copy = _setFunc(copy, _parameters);
                    }
                    else
                    {
                        _setMethod.Invoke(copy, _parameters);
                    }
                    if (copy != null)
                    {
                        _operandLocation.Value = (TOperand)copy;
                    }
                }
            }

            [DataMember(EmitDefaultValue = false, Name = "operandLocation")]
            internal Location<TOperand> SerializedOperandLocation
            {
                get { return _operandLocation; }
                set { _operandLocation = value; }
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
