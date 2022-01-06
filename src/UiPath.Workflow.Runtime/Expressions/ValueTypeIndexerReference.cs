// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Runtime.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Windows.Markup;

namespace System.Activities.Expressions;

[ContentProperty("Indices")]
public sealed class ValueTypeIndexerReference<TOperand, TItem> : CodeActivity<Location<TItem>>
{
    private Collection<InArgument> _indices;
    private MethodInfo _getMethod;
    private MethodInfo _setMethod;
    private Func<object, object[], object> _getFunc;
    private Func<object, object[], object> _setFunc;
    private static readonly MruCache<MethodInfo, Func<object, object[], object>> funcCache =
        new(MethodCallExpressionHelper.FuncCacheCapacity);
    private static readonly ReaderWriterLockSlim locker = new();

    [RequiredArgument]
    [DefaultValue(null)]
    public InOutArgument<TOperand> OperandLocation { get; set; }

    [RequiredArgument]
    [DefaultValue(null)]
    public Collection<InArgument> Indices
    {
        get
        {
            _indices ??= new ValidatingCollection<InArgument>
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
        MethodInfo oldGetMethod = _getMethod;
        MethodInfo oldSetMethod = _setMethod;
        if (!typeof(TOperand).IsValueType)
        {
            metadata.AddValidationError(SR.TypeMustbeValueType(typeof(TOperand).Name));
        }
        if (Indices.Count == 0)
        {
            metadata.AddValidationError(SR.IndicesAreNeeded(GetType().Name, DisplayName));
        }
        else
        {
            IndexerHelper.CacheMethod<TOperand, TItem>(Indices, ref _getMethod, ref _setMethod);
            if (_setMethod == null)
            {
                metadata.AddValidationError(SR.SpecialMethodNotFound("set_Item", typeof(TOperand).Name));
            }
        }

        RuntimeArgument operandArgument = new("OperandLocation", typeof(TOperand), ArgumentDirection.InOut, true);
        metadata.Bind(OperandLocation, operandArgument);
        metadata.AddArgument(operandArgument);

        IndexerHelper.OnGetArguments(Indices, Result, metadata);

        if (MethodCallExpressionHelper.NeedRetrieve(_getMethod, oldGetMethod, _getFunc))
        {
            _getFunc = MethodCallExpressionHelper.GetFunc(metadata, _getMethod, funcCache, locker);
        }
        if (MethodCallExpressionHelper.NeedRetrieve(_setMethod, oldSetMethod, _setFunc))
        {
            _setFunc = MethodCallExpressionHelper.GetFunc(metadata, _setMethod, funcCache, locker, true);
        }
    }

    protected override Location<TItem> Execute(CodeActivityContext context)
    {
        object[] indicesValue = new object[Indices.Count];
        for (int i = 0; i < Indices.Count; i++)
        {
            indicesValue[i] = Indices[i].Get(context);
        }
        Location<TOperand> operandLocationValue = OperandLocation.GetLocation(context);
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
        private readonly Func<object, object[], object> _getFunc;
        private readonly Func<object, object[], object> _setFunc;

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
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.SpecialMethodNotFound("get_Item", typeof(TOperand).Name)));
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
                    _parameters[^1] = value;
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
            get => _operandLocation;
            set => _operandLocation = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "indices")]
        internal object[] SerializedIndices
        {
            get => _indices;
            set => _indices = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "parameters")]
        internal object[] SerializedParameters
        {
            get => _parameters;
            set => _parameters = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "getMethod")]
        internal MethodInfo SerializedGetMethod
        {
            get => _getMethod;
            set => _getMethod = value;
        }

        [DataMember(EmitDefaultValue = false, Name = "setMethod")]
        internal MethodInfo SerializedSetMethod
        {
            get => _setMethod;
            set => _setMethod = value;
        }
    }
}
