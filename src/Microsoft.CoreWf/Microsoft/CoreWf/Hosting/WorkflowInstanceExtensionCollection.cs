// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CoreWf.Hosting
{
    internal class WorkflowInstanceExtensionCollection
    {
        private List<KeyValuePair<WorkflowInstanceExtensionProvider, object>> _instanceExtensions;
        private List<object> _additionalInstanceExtensions;
        private List<object> _allSingletonExtensions;
        private bool _hasTrackingParticipant;
        private bool _hasPersistenceModule;
        private bool _shouldSetInstanceForInstanceExtensions;

        // cache for cases where we have a single match
        private Dictionary<Type, object> _singleTypeCache;

        private List<IWorkflowInstanceExtension> _workflowInstanceExtensions;

        // optimization for common extension in a loop/parallel (like Compensation or Send)
        private Type _lastTypeCached;
        private object _lastObjectCached;

        // temporary pointer to our parent manager between ctor and Initialize
        private WorkflowInstanceExtensionManager _extensionManager;

        internal WorkflowInstanceExtensionCollection(Activity workflowDefinition, WorkflowInstanceExtensionManager extensionManager)
        {
            _extensionManager = extensionManager;

            int extensionProviderCount = 0;
            if (extensionManager != null)
            {
                extensionProviderCount = extensionManager.ExtensionProviders.Count;
                _hasTrackingParticipant = extensionManager.HasSingletonTrackingParticipant;
                _hasPersistenceModule = extensionManager.HasSingletonPersistenceModule;

                // create an uber-IEnumerable to simplify our iteration code
                _allSingletonExtensions = _extensionManager.GetAllSingletonExtensions();
            }
            else
            {
                _allSingletonExtensions = WorkflowInstanceExtensionManager.EmptySingletonExtensions;
            }

            // Resolve activity extensions
            Dictionary<Type, WorkflowInstanceExtensionProvider> activityExtensionProviders;
            Dictionary<Type, WorkflowInstanceExtensionProvider> filteredActivityExtensionProviders = null;
            HashSet<Type> requiredActivityExtensionTypes;
            if (workflowDefinition.GetActivityExtensionInformation(out activityExtensionProviders, out requiredActivityExtensionTypes))
            {
                // a) filter out the extension Types that were already configured by the host. Note that only "primary" extensions are in play here, not
                // "additional" extensions
                HashSet<Type> allExtensionTypes = new HashSet<Type>();
                if (extensionManager != null)
                {
                    extensionManager.AddAllExtensionTypes(allExtensionTypes);
                }

                if (activityExtensionProviders != null)
                {
                    filteredActivityExtensionProviders = new Dictionary<Type, WorkflowInstanceExtensionProvider>(activityExtensionProviders.Count);
                    foreach (KeyValuePair<Type, WorkflowInstanceExtensionProvider> keyedActivityExtensionProvider in activityExtensionProviders)
                    {
                        Type newExtensionProviderType = keyedActivityExtensionProvider.Key;
                        if (!TypeHelper.ContainsCompatibleType(allExtensionTypes, newExtensionProviderType))
                        {
                            // first see if the new provider supersedes any existing ones
                            List<Type> typesToRemove = null;
                            bool skipNewExtensionProvider = false;
                            foreach (Type existingExtensionProviderType in filteredActivityExtensionProviders.Keys)
                            {
                                // Use AreReferenceTypesCompatible for performance since we know that all of these must be reference types
                                if (TypeHelper.AreReferenceTypesCompatible(existingExtensionProviderType, newExtensionProviderType))
                                {
                                    skipNewExtensionProvider = true;
                                    break;
                                }

                                if (TypeHelper.AreReferenceTypesCompatible(newExtensionProviderType, existingExtensionProviderType))
                                {
                                    if (typesToRemove == null)
                                    {
                                        typesToRemove = new List<Type>();
                                    }
                                    typesToRemove.Add(existingExtensionProviderType);
                                }
                            }

                            // prune unnecessary extension providers (either superseded by the new extension or by an existing extension that supersedes them both)
                            if (typesToRemove != null)
                            {
                                for (int i = 0; i < typesToRemove.Count; i++)
                                {
                                    filteredActivityExtensionProviders.Remove(typesToRemove[i]);
                                }
                            }

                            // and add a new extension if necessary
                            if (!skipNewExtensionProvider)
                            {
                                filteredActivityExtensionProviders.Add(newExtensionProviderType, keyedActivityExtensionProvider.Value);
                            }
                        }
                    }
                    if (filteredActivityExtensionProviders.Count > 0)
                    {
                        allExtensionTypes.UnionWith(filteredActivityExtensionProviders.Keys);
                        extensionProviderCount += filteredActivityExtensionProviders.Count;
                    }
                }

                // b) Validate that all required extensions will be provided
                if (requiredActivityExtensionTypes != null && requiredActivityExtensionTypes.Count > 0)
                {
                    foreach (Type requiredType in requiredActivityExtensionTypes)
                    {
                        if (!TypeHelper.ContainsCompatibleType(allExtensionTypes, requiredType))
                        {
                            throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new ValidationException(SR.RequiredExtensionTypeNotFound(requiredType.ToString())));
                        }
                    }
                }
            }

            // Finally, if our checks of passed, resolve our delegates
            if (extensionProviderCount > 0)
            {
                _instanceExtensions = new List<KeyValuePair<WorkflowInstanceExtensionProvider, object>>(extensionProviderCount);

                if (extensionManager != null)
                {
                    List<KeyValuePair<Type, WorkflowInstanceExtensionProvider>> extensionProviders = extensionManager.ExtensionProviders;
                    for (int i = 0; i < extensionProviders.Count; i++)
                    {
                        KeyValuePair<Type, WorkflowInstanceExtensionProvider> extensionProvider = extensionProviders[i];
                        AddInstanceExtension(extensionProvider.Value);
                    }
                }

                if (filteredActivityExtensionProviders != null)
                {
                    foreach (WorkflowInstanceExtensionProvider extensionProvider in filteredActivityExtensionProviders.Values)
                    {
                        AddInstanceExtension(extensionProvider);
                    }
                }
            }
        }

        private void AddInstanceExtension(WorkflowInstanceExtensionProvider extensionProvider)
        {
            Fx.Assert(_instanceExtensions != null, "instanceExtensions should be setup by now");
            object newExtension = extensionProvider.ProvideValue();
            if (newExtension is SymbolResolver)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.SymbolResolverMustBeSingleton));
            }

            // for IWorkflowInstance we key off the type of the value, not the declared type
            if (!_shouldSetInstanceForInstanceExtensions && newExtension is IWorkflowInstanceExtension)
            {
                _shouldSetInstanceForInstanceExtensions = true;
            }
            if (!_hasTrackingParticipant && extensionProvider.IsMatch<TrackingParticipant>(newExtension))
            {
                _hasTrackingParticipant = true;
            }
            if (!_hasPersistenceModule && extensionProvider.IsMatch<IPersistencePipelineModule>(newExtension))
            {
                _hasPersistenceModule = true;
            }

            _instanceExtensions.Add(new KeyValuePair<WorkflowInstanceExtensionProvider, object>(extensionProvider, newExtension));

            WorkflowInstanceExtensionManager.AddExtensionClosure(newExtension, ref _additionalInstanceExtensions, ref _hasTrackingParticipant, ref _hasPersistenceModule);
        }

        internal bool HasPersistenceModule
        {
            get
            {
                return _hasPersistenceModule;
            }
        }

        internal bool HasTrackingParticipant
        {
            get
            {
                return _hasTrackingParticipant;
            }
        }

        public bool HasWorkflowInstanceExtensions
        {
            get
            {
                return _workflowInstanceExtensions != null && _workflowInstanceExtensions.Count > 0;
            }
        }

        public List<IWorkflowInstanceExtension> WorkflowInstanceExtensions
        {
            get
            {
                return _workflowInstanceExtensions;
            }
        }

        internal void Initialize()
        {
            if (_extensionManager != null)
            {
                // if we have any singleton IWorkflowInstanceExtensions, initialize them first
                // All validation logic for singletons is done through WorkflowInstanceExtensionManager
                if (_extensionManager.HasSingletonIWorkflowInstanceExtensions)
                {
                    SetInstance(_extensionManager.SingletonExtensions);

                    if (_extensionManager.HasAdditionalSingletonIWorkflowInstanceExtensions)
                    {
                        SetInstance(_extensionManager.AdditionalSingletonExtensions);
                    }
                }
            }

            if (_shouldSetInstanceForInstanceExtensions)
            {
                for (int i = 0; i < _instanceExtensions.Count; i++)
                {
                    KeyValuePair<WorkflowInstanceExtensionProvider, object> keyedExtension = _instanceExtensions[i];
                    // for IWorkflowInstance we key off the type of the value, not the declared type
                    IWorkflowInstanceExtension workflowInstanceExtension = keyedExtension.Value as IWorkflowInstanceExtension;

                    if (workflowInstanceExtension != null)
                    {
                        if (_workflowInstanceExtensions == null)
                        {
                            _workflowInstanceExtensions = new List<IWorkflowInstanceExtension>();
                        }

                        _workflowInstanceExtensions.Add(workflowInstanceExtension);
                    }
                }

                if (_additionalInstanceExtensions != null)
                {
                    SetInstance(_additionalInstanceExtensions);
                }
            }
        }

        private void SetInstance(List<object> extensionsList)
        {
            for (int i = 0; i < extensionsList.Count; i++)
            {
                object extension = extensionsList[i];
                if (extension is IWorkflowInstanceExtension)
                {
                    if (_workflowInstanceExtensions == null)
                    {
                        _workflowInstanceExtensions = new List<IWorkflowInstanceExtension>();
                    }

                    _workflowInstanceExtensions.Add((IWorkflowInstanceExtension)extension);
                }
            }
        }

        public T Find<T>()
            where T : class
        {
            T result = null;

            object cachedExtension;
            if (TryGetCachedExtension(typeof(T), out cachedExtension))
            {
                return (T)cachedExtension;
            }

            try
            {
                // when we have support for context.GetExtensions<T>(), then change from early break to ThrowOnMultipleMatches ("There are more than one matched extensions found which is not allowed with GetExtension method call. Please use GetExtensions method instead.")
                for (int i = 0; i < _allSingletonExtensions.Count; i++)
                {
                    object extension = _allSingletonExtensions[i];
                    result = extension as T;
                    if (result != null)
                    {
                        return result;
                    }
                }

                if (_instanceExtensions != null)
                {
                    for (int i = 0; i < _instanceExtensions.Count; i++)
                    {
                        KeyValuePair<WorkflowInstanceExtensionProvider, object> keyedExtension = _instanceExtensions[i];
                        if (keyedExtension.Key.IsMatch<T>(keyedExtension.Value))
                        {
                            result = (T)keyedExtension.Value;
                            return result;
                        }
                    }

                    if (_additionalInstanceExtensions != null)
                    {
                        for (int i = 0; i < _additionalInstanceExtensions.Count; i++)
                        {
                            object additionalExtension = _additionalInstanceExtensions[i];
                            result = additionalExtension as T;
                            if (result != null)
                            {
                                return result;
                            }
                        }
                    }
                }

                return result;
            }
            finally
            {
                CacheExtension(result);
            }
        }

        public IEnumerable<T> FindAll<T>()
            where T : class
        {
            return FindAll<T>(false);
        }

        private IEnumerable<T> FindAll<T>(bool useObjectTypeForComparison)
            where T : class
        {
            // sometimes we match the single case even when you ask for multiple
            object cachedExtension;
            if (TryGetCachedExtension(typeof(T), out cachedExtension))
            {
                yield return (T)cachedExtension;
            }
            else
            {
                T lastExtension = null;
                bool hasMultiple = false;

                foreach (T extension in _allSingletonExtensions.OfType<T>())
                {
                    if (lastExtension == null)
                    {
                        lastExtension = extension;
                    }
                    else
                    {
                        hasMultiple = true;
                    }

                    yield return extension;
                }

                foreach (T extension in GetInstanceExtensions<T>(useObjectTypeForComparison))
                {
                    if (lastExtension == null)
                    {
                        lastExtension = extension;
                    }
                    else
                    {
                        hasMultiple = true;
                    }

                    yield return extension;
                }

                if (!hasMultiple)
                {
                    CacheExtension(lastExtension);
                }
            }
        }

        private IEnumerable<T> GetInstanceExtensions<T>(bool useObjectTypeForComparison) where T : class
        {
            if (_instanceExtensions != null)
            {
                for (int i = 0; i < _instanceExtensions.Count; i++)
                {
                    KeyValuePair<WorkflowInstanceExtensionProvider, object> keyedExtension = _instanceExtensions[i];
                    if ((useObjectTypeForComparison && keyedExtension.Value is T)
                        || keyedExtension.Key.IsMatch<T>(keyedExtension.Value))
                    {
                        yield return (T)keyedExtension.Value;
                    }
                }

                if (_additionalInstanceExtensions != null)
                {
                    foreach (object additionalExtension in _additionalInstanceExtensions)
                    {
                        if (additionalExtension is T)
                        {
                            yield return (T)additionalExtension;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            // we should only call dispose on instance extensions, since those are
            // the only ones we created
            foreach (IDisposable disposableExtension in GetInstanceExtensions<IDisposable>(true))
            {
                disposableExtension.Dispose();
            }
        }

        public void Cancel()
        {
            foreach (ICancelable cancelableExtension in GetInstanceExtensions<ICancelable>(true))
            {
                cancelableExtension.Cancel();
            }
        }

        private void CacheExtension<T>(T extension)
            where T : class
        {
            if (extension != null)
            {
                CacheExtension(typeof(T), extension);
            }
        }

        private void CacheExtension(Type extensionType, object extension)
        {
            if (extension != null)
            {
                if (_singleTypeCache == null)
                {
                    _singleTypeCache = new Dictionary<Type, object>();
                }

                _lastTypeCached = extensionType;
                _lastObjectCached = extension;
                _singleTypeCache[extensionType] = extension;
            }
        }

        private bool TryGetCachedExtension(Type type, out object extension)
        {
            if (_singleTypeCache == null)
            {
                extension = null;
                return false;
            }

            if (object.ReferenceEquals(type, _lastTypeCached))
            {
                extension = _lastObjectCached;
                return true;
            }

            return _singleTypeCache.TryGetValue(type, out extension);
        }
    }
}
