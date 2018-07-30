// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using CoreWf.Runtime.DurableInstancing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace CoreWf.Runtime
{
    internal class PersistencePipeline
    {
        private readonly IEnumerable<IPersistencePipelineModule> _modules;

        private Stage _expectedStage;

        private IDictionary<XName, InstanceValue> _values;
        private ReadOnlyDictionary<XName, InstanceValue> _readOnlyView;
        private ValueDictionaryView _readWriteView;
        private ValueDictionaryView _writeOnlyView;

        // Used for the save pipeline.
        public PersistencePipeline(IEnumerable<IPersistencePipelineModule> modules, Dictionary<XName, InstanceValue> initialValues)
        {
            Fx.Assert(modules != null, "Null modules collection provided to persistence pipeline.");

            _expectedStage = Stage.Collect;
            _modules = modules;
            _values = initialValues;
            _readOnlyView = new ReadOnlyDictionary<XName, InstanceValue>(_values);
            _readWriteView = new ValueDictionaryView(_values, false);
            _writeOnlyView = new ValueDictionaryView(_values, true);
        }

        // Used for the load pipeline.
        public PersistencePipeline(IEnumerable<IPersistencePipelineModule> modules)
        {
            Fx.Assert(modules != null, "Null modules collection provided to persistence pipeline.");

            _expectedStage = Stage.Load;
            _modules = modules;
        }

        public ReadOnlyDictionary<XName, InstanceValue> Values
        {
            get
            {
                return _readOnlyView;
            }
        }

        public bool IsSaveTransactionRequired
        {
            get
            {
                return _modules.FirstOrDefault(value => value.IsSaveTransactionRequired) != null;
            }
        }

        public bool IsLoadTransactionRequired
        {
            get
            {
                return _modules.FirstOrDefault(value => value.IsLoadTransactionRequired) != null;
            }
        }

        public void Collect()
        {
            Fx.AssertAndThrow(_expectedStage == Stage.Collect, "Collect called at the wrong time.");
            _expectedStage = Stage.None;

            foreach (IPersistencePipelineModule module in _modules)
            {

                module.CollectValues(out IDictionary<XName, object> readWriteValues, out IDictionary<XName, object> writeOnlyValues);
                if (readWriteValues != null)
                {
                    foreach (KeyValuePair<XName, object> value in readWriteValues)
                    {
                        try
                        {
                            _values.Add(value.Key, new InstanceValue(value.Value));
                        }
                        catch (ArgumentException exception)
                        {
                            throw Fx.Exception.AsError(new InvalidOperationException(SR.NameCollisionOnCollect(value.Key, module.GetType().Name), exception));
                        }
                    }
                }
                if (writeOnlyValues != null)
                {
                    foreach (KeyValuePair<XName, object> value in writeOnlyValues)
                    {
                        try
                        {
                            _values.Add(value.Key, new InstanceValue(value.Value, InstanceValueOptions.Optional | InstanceValueOptions.WriteOnly));
                        }
                        catch (ArgumentException exception)
                        {
                            throw Fx.Exception.AsError(new InvalidOperationException(SR.NameCollisionOnCollect(value.Key, module.GetType().Name), exception));
                        }
                    }
                }
            }

            _expectedStage = Stage.Map;
        }

        public void Map()
        {
            Fx.AssertAndThrow(_expectedStage == Stage.Map, "Map called at the wrong time.");
            _expectedStage = Stage.None;

            List<Tuple<IPersistencePipelineModule, IDictionary<XName, object>>> pendingValues = null;

            foreach (IPersistencePipelineModule module in _modules)
            {
                IDictionary<XName, object> mappedValues = module.MapValues(_readWriteView, _writeOnlyView);
                if (mappedValues != null)
                {
                    if (pendingValues == null)
                    {
                        pendingValues = new List<Tuple<IPersistencePipelineModule, IDictionary<XName, object>>>();
                    }
                    pendingValues.Add(new Tuple<IPersistencePipelineModule, IDictionary<XName, object>>(module, mappedValues));
                }
            }

            if (pendingValues != null)
            {
                foreach (Tuple<IPersistencePipelineModule, IDictionary<XName, object>> writeOnlyValues in pendingValues)
                {
                    foreach (KeyValuePair<XName, object> value in writeOnlyValues.Item2)
                    {
                        try
                        {
                            _values.Add(value.Key, new InstanceValue(value.Value, InstanceValueOptions.Optional | InstanceValueOptions.WriteOnly));
                        }
                        catch (ArgumentException exception)
                        {
                            throw Fx.Exception.AsError(new InvalidOperationException(SR.NameCollisionOnMap(value.Key, writeOnlyValues.Item1.GetType().Name), exception));
                        }
                    }
                }

                _writeOnlyView.ResetCaches();
            }

            _expectedStage = Stage.Save;
        }

        public IAsyncResult BeginSave(TimeSpan timeout, AsyncCallback callback, object state)
        {
            Fx.AssertAndThrow(_expectedStage == Stage.Save, "Save called at the wrong time.");
            _expectedStage = Stage.None;

            return new IOAsyncResult(this, false, timeout, callback, state);
        }

        public void EndSave(IAsyncResult result)
        {
            IOAsyncResult.End(result);
        }

        public void SetLoadedValues(IDictionary<XName, InstanceValue> values)
        {
            Fx.AssertAndThrow(_expectedStage == Stage.Load, "SetLoadedValues called at the wrong time.");
            Fx.Assert(values != null, "Null values collection provided to SetLoadedValues.");

            _values = values;
            _readOnlyView = values as ReadOnlyDictionary<XName, InstanceValue> ?? new ReadOnlyDictionary<XName, InstanceValue>(values);
            _readWriteView = new ValueDictionaryView(_values, false);
        }

        public IAsyncResult BeginLoad(TimeSpan timeout, AsyncCallback callback, object state)
        {
            Fx.Assert(_values != null, "SetLoadedValues not called.");
            Fx.AssertAndThrow(_expectedStage == Stage.Load, "Load called at the wrong time.");
            _expectedStage = Stage.None;

            return new IOAsyncResult(this, true, timeout, callback, state);
        }

        public void EndLoad(IAsyncResult result)
        {
            IOAsyncResult.End(result);
            _expectedStage = Stage.Publish;
        }

        public void Publish()
        {
            Fx.AssertAndThrow(_expectedStage == Stage.Publish || _expectedStage == Stage.Load, "Publish called at the wrong time.");
            _expectedStage = Stage.None;

            foreach (IPersistencePipelineModule module in _modules)
            {
                module.PublishValues(_readWriteView);
            }
        }

        public void Abort()
        {
            foreach (IPersistencePipelineModule module in _modules)
            {
                try
                {
                    module.Abort();
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }
                    throw Fx.Exception.AsError(new CallbackException(SR.PersistencePipelineAbortThrew(module.GetType().Name), exception));
                }
            }
        }

        private enum Stage
        {
            None,
            Collect,
            Map,
            Save,
            Load,
            Publish,
        }

        private class ValueDictionaryView : IDictionary<XName, object>
        {
            private IDictionary<XName, InstanceValue> _basis;
            private readonly bool _writeOnly;

            private List<XName> _keys;
            private List<object> _values;

            public ValueDictionaryView(IDictionary<XName, InstanceValue> basis, bool writeOnly)
            {
                _basis = basis;
                _writeOnly = writeOnly;
            }

            public ICollection<XName> Keys
            {
                get
                {
                    if (_keys == null)
                    {
                        _keys = new List<XName>(_basis.Where(value => value.Value.IsWriteOnly() == _writeOnly).Select(value => value.Key));
                    }
                    return _keys;
                }
            }

            public ICollection<object> Values
            {
                get
                {
                    if (_values == null)
                    {
                        _values = new List<object>(_basis.Where(value => value.Value.IsWriteOnly() == _writeOnly).Select(value => value.Value.Value));
                    }
                    return _values;
                }
            }

            public object this[XName key]
            {
                get
                {
                    if (TryGetValue(key, out object value))
                    {
                        return value;
                    }
                    throw Fx.Exception.AsError(new KeyNotFoundException());
                }

                set
                {
                    throw Fx.Exception.AsError(CreateReadOnlyException());
                }
            }

            public int Count
            {
                get
                {
                    return Keys.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return true;
                }
            }

            public void Add(XName key, object value)
            {
                throw Fx.Exception.AsError(CreateReadOnlyException());
            }

            public bool ContainsKey(XName key)
            {
                return TryGetValue(key, out object dummy);
            }

            public bool Remove(XName key)
            {
                throw Fx.Exception.AsError(CreateReadOnlyException());
            }

            public bool TryGetValue(XName key, out object value)
            {
                if (!_basis.TryGetValue(key, out InstanceValue realValue) || realValue.IsWriteOnly() != _writeOnly)
                {
                    value = null;
                    return false;
                }

                value = realValue.Value;
                return true;
            }

            public void Add(KeyValuePair<XName, object> item)
            {
                throw Fx.Exception.AsError(CreateReadOnlyException());
            }

            public void Clear()
            {
                throw Fx.Exception.AsError(CreateReadOnlyException());
            }

            public bool Contains(KeyValuePair<XName, object> item)
            {
                if (!TryGetValue(item.Key, out object value))
                {
                    return false;
                }
                return EqualityComparer<object>.Default.Equals(value, item.Value);
            }

            public void CopyTo(KeyValuePair<XName, object>[] array, int arrayIndex)
            {
                foreach (KeyValuePair<XName, object> entry in this)
                {
                    array[arrayIndex++] = entry;
                }
            }

            public bool Remove(KeyValuePair<XName, object> item)
            {
                throw Fx.Exception.AsError(CreateReadOnlyException());
            }

            public IEnumerator<KeyValuePair<XName, object>> GetEnumerator()
            {
                return _basis.Where(value => value.Value.IsWriteOnly() == _writeOnly).Select(value => new KeyValuePair<XName, object>(value.Key, value.Value.Value)).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            internal void ResetCaches()
            {
                _keys = null;
                _values = null;
            }

            private Exception CreateReadOnlyException()
            {
                return new InvalidOperationException(SR.DictionaryIsReadOnly);
            }
        }

        private class IOAsyncResult : AsyncResult
        {
            private PersistencePipeline _pipeline;
            private readonly bool _isLoad;
            private IPersistencePipelineModule[] _pendingModules;
            private int _remainingModules;
            private Exception _exception;

            public IOAsyncResult(PersistencePipeline pipeline, bool isLoad, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                _pipeline = pipeline;
                _isLoad = isLoad;
                _pendingModules = _pipeline._modules.Where(value => value.IsIOParticipant).ToArray();
                _remainingModules = _pendingModules.Length;

                bool completeSelf = false;
                if (_pendingModules.Length == 0)
                {
                    completeSelf = true;
                }
                else
                {
                    for (int i = 0; i < _pendingModules.Length; i++)
                    {
                        Fx.Assert(!completeSelf, "Shouldn't have been completed yet.");

                        IPersistencePipelineModule module = _pendingModules[i];
                        IAsyncResult result = null;
                        try
                        {
                            if (_isLoad)
                            {
                                result = module.BeginOnLoad(_pipeline._readWriteView, timeout, Fx.ThunkCallback(new AsyncCallback(OnIOComplete)), i);
                            }
                            else
                            {
                                result = module.BeginOnSave(_pipeline._readWriteView, _pipeline._writeOnlyView, timeout, Fx.ThunkCallback(new AsyncCallback(OnIOComplete)), i);
                            }
                        }
                        catch (Exception exception)
                        {
                            if (Fx.IsFatal(exception))
                            {
                                throw;
                            }

                            _pendingModules[i] = null;
                            ProcessException(exception);
                        }
                        if (result == null)
                        {
                            if (CompleteOne())
                            {
                                completeSelf = true;
                            }
                        }
                        else if (result.CompletedSynchronously)
                        {
                            _pendingModules[i] = null;
                            if (IOComplete(result, module))
                            {
                                completeSelf = true;
                            }
                        }
                    }
                }

                if (completeSelf)
                {
                    Complete(true, _exception);
                }
            }

            private void OnIOComplete(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                int i = (int)result.AsyncState;

                IPersistencePipelineModule module = _pendingModules[i];
                Fx.Assert(module != null, "There should be a pending result for this result");
                _pendingModules[i] = null;

                if (IOComplete(result, module))
                {
                    Complete(false, _exception);
                }
            }

            private bool IOComplete(IAsyncResult result, IPersistencePipelineModule module)
            {
                try
                {
                    if (_isLoad)
                    {
                        module.EndOnLoad(result);
                    }
                    else
                    {
                        module.EndOnSave(result);
                    }
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    ProcessException(exception);
                }
                return CompleteOne();
            }

            private void ProcessException(Exception exception)
            {
                if (exception != null)
                {
                    bool abortNeeded = false;
                    lock (_pendingModules)
                    {
                        if (_exception == null)
                        {
                            _exception = exception;
                            abortNeeded = true;
                        }
                    }

                    if (abortNeeded)
                    {
                        Abort();
                    }
                }
            }

            private bool CompleteOne()
            {
                return Interlocked.Decrement(ref _remainingModules) == 0;
            }

            private void Abort()
            {
                for (int j = 0; j < _pendingModules.Length; j++)
                {
                    IPersistencePipelineModule module = _pendingModules[j];
                    if (module != null)
                    {
                        try
                        {
                            module.Abort();
                        }
                        catch (Exception exception)
                        {
                            if (Fx.IsFatal(exception))
                            {
                                throw;
                            }
                            throw Fx.Exception.AsError(new CallbackException(SR.PersistencePipelineAbortThrew(module.GetType().Name), exception));
                        }
                    }
                }
            }

            public static void End(IAsyncResult result)
            {
                AsyncResult.End<IOAsyncResult>(result);
            }
        }
    }
}
