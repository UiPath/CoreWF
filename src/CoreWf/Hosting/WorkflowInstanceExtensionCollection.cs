// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Hosting
{
    using CoreWf.Internals;
    using CoreWf.Runtime;
    using CoreWf.Tracking;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class WorkflowInstanceExtensionCollection
    {
        private readonly List<KeyValuePair<WorkflowInstanceExtensionProvider, object>> instanceExtensions;
        private List<object> additionalInstanceExtensions;
        private readonly List<object> allSingletonExtensions;
        private bool hasTrackingParticipant;
        private bool hasPersistenceModule;
        private bool shouldSetInstanceForInstanceExtensions;

        // cache for cases where we have a single match
        private Dictionary<Type, object> singleTypeCache;
        private List<IWorkflowInstanceExtension> workflowInstanceExtensions;

        // optimization for common extension in a loop/parallel (like Compensation or Send)
        private Type lastTypeCached;
        private object lastObjectCached;

        // temporary pointer to our parent manager between ctor and Initialize
        private readonly WorkflowInstanceExtensionManager extensionManager;

        internal WorkflowInstanceExtensionCollection(Activity workflowDefinition, WorkflowInstanceExtensionManager extensionManager)
        {
            this.extensionManager = extensionManager;

            int extensionProviderCount = 0;
            if (extensionManager != null)
            {
                extensionProviderCount = extensionManager.ExtensionProviders.Count;
                this.hasTrackingParticipant = extensionManager.HasSingletonTrackingParticipant;
                this.hasPersistenceModule = extensionManager.HasSingletonPersistenceModule;

                // create an uber-IEnumerable to simplify our iteration code
                this.allSingletonExtensions = this.extensionManager.GetAllSingletonExtensions();
            }
            else
            {
                this.allSingletonExtensions = WorkflowInstanceExtensionManager.EmptySingletonExtensions;
            }

            // Resolve activity extensions
            Dictionary<Type, WorkflowInstanceExtensionProvider> filteredActivityExtensionProviders = null;
            if (workflowDefinition.GetActivityExtensionInformation(out Dictionary<Type, WorkflowInstanceExtensionProvider> activityExtensionProviders, out HashSet<Type> requiredActivityExtensionTypes))
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
                            throw FxTrace.Exception.AsError(new ValidationException(SR.RequiredExtensionTypeNotFound(requiredType.ToString())));
                        }
                    }
                }
            }

            // Finally, if our checks of passed, resolve our delegates
            if (extensionProviderCount > 0)
            {
                this.instanceExtensions = new List<KeyValuePair<WorkflowInstanceExtensionProvider, object>>(extensionProviderCount);

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
            Fx.Assert(this.instanceExtensions != null, "instanceExtensions should be setup by now");
            object newExtension = extensionProvider.ProvideValue();
            if (newExtension is SymbolResolver)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.SymbolResolverMustBeSingleton));
            }

            // for IWorkflowInstance we key off the type of the value, not the declared type
            if (!this.shouldSetInstanceForInstanceExtensions && newExtension is IWorkflowInstanceExtension)
            {
                this.shouldSetInstanceForInstanceExtensions = true;
            }
            if (!this.hasTrackingParticipant && extensionProvider.IsMatch<TrackingParticipant>(newExtension))
            {
                this.hasTrackingParticipant = true;
            }
            if (!this.hasPersistenceModule && extensionProvider.IsMatch<IPersistencePipelineModule>(newExtension))
            {
                this.hasPersistenceModule = true;
            }

            this.instanceExtensions.Add(new KeyValuePair<WorkflowInstanceExtensionProvider, object>(extensionProvider, newExtension));

            WorkflowInstanceExtensionManager.AddExtensionClosure(newExtension, ref this.additionalInstanceExtensions, ref this.hasTrackingParticipant, ref this.hasPersistenceModule);
        }

        internal bool HasPersistenceModule
        {
            get
            {
                return this.hasPersistenceModule;
            }
        }

        internal bool HasTrackingParticipant
        {
            get
            {
                return this.hasTrackingParticipant;
            }
        }

        public bool HasWorkflowInstanceExtensions
        {
            get
            {
                return this.workflowInstanceExtensions != null && this.workflowInstanceExtensions.Count > 0;
            }
        }

        public List<IWorkflowInstanceExtension> WorkflowInstanceExtensions
        {
            get
            {
                return this.workflowInstanceExtensions;
            }
        }

        internal void Initialize()
        {
            if (this.extensionManager != null)
            {
                // if we have any singleton IWorkflowInstanceExtensions, initialize them first
                // All validation logic for singletons is done through WorkflowInstanceExtensionManager
                if (this.extensionManager.HasSingletonIWorkflowInstanceExtensions)
                {
                    SetInstance(this.extensionManager.SingletonExtensions);

                    if (this.extensionManager.HasAdditionalSingletonIWorkflowInstanceExtensions)
                    {
                        SetInstance(this.extensionManager.AdditionalSingletonExtensions);
                    }
                }
            }

            if (this.shouldSetInstanceForInstanceExtensions)
            {
                for (int i = 0; i < this.instanceExtensions.Count; i++)
                {
                    KeyValuePair<WorkflowInstanceExtensionProvider, object> keyedExtension = this.instanceExtensions[i];
                    // for IWorkflowInstance we key off the type of the value, not the declared type

                    if (keyedExtension.Value is IWorkflowInstanceExtension workflowInstanceExtension)
                    {
                        if (this.workflowInstanceExtensions == null)
                        {
                            this.workflowInstanceExtensions = new List<IWorkflowInstanceExtension>();
                        }

                        this.workflowInstanceExtensions.Add(workflowInstanceExtension);
                    }
                }

                if (this.additionalInstanceExtensions != null)
                {
                    SetInstance(this.additionalInstanceExtensions);
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
                    if (this.workflowInstanceExtensions == null)
                    {
                        this.workflowInstanceExtensions = new List<IWorkflowInstanceExtension>();
                    }

                    this.workflowInstanceExtensions.Add((IWorkflowInstanceExtension)extension);
                }
            }
        }

        public T Find<T>()
            where T : class
        {
            T result = null;

            if (TryGetCachedExtension(typeof(T), out object cachedExtension))
            {
                return (T)cachedExtension;
            }

            try
            {
                // when we have support for context.GetExtensions<T>(), then change from early break to ThrowOnMultipleMatches ("There are more than one matched extensions found which is not allowed with GetExtension method call. Please use GetExtensions method instead.")
                for (int i = 0; i < this.allSingletonExtensions.Count; i++)
                {
                    object extension = this.allSingletonExtensions[i];
                    result = extension as T;
                    if (result != null)
                    {
                        return result;
                    }
                }

                if (this.instanceExtensions != null)
                {
                    for (int i = 0; i < this.instanceExtensions.Count; i++)
                    {
                        KeyValuePair<WorkflowInstanceExtensionProvider, object> keyedExtension = this.instanceExtensions[i];
                        if (keyedExtension.Key.IsMatch<T>(keyedExtension.Value))
                        {
                            result = (T)keyedExtension.Value;
                            return result;
                        }
                    }

                    if (this.additionalInstanceExtensions != null)
                    {
                        for (int i = 0; i < this.additionalInstanceExtensions.Count; i++)
                        {
                            object additionalExtension = this.additionalInstanceExtensions[i];
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
            if (TryGetCachedExtension(typeof(T), out object cachedExtension))
            {
                yield return (T)cachedExtension;
            }
            else
            {
                T lastExtension = null;
                bool hasMultiple = false;

                foreach (T extension in this.allSingletonExtensions.OfType<T>())
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
            if (this.instanceExtensions != null)
            {
                for (int i = 0; i < this.instanceExtensions.Count; i++)
                {
                    KeyValuePair<WorkflowInstanceExtensionProvider, object> keyedExtension = this.instanceExtensions[i];
                    if ((useObjectTypeForComparison && keyedExtension.Value is T)
                        || keyedExtension.Key.IsMatch<T>(keyedExtension.Value))
                    {
                        yield return (T)keyedExtension.Value;
                    }
                }

                if (this.additionalInstanceExtensions != null)
                {
                    foreach (object additionalExtension in this.additionalInstanceExtensions)
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
                if (this.singleTypeCache == null)
                {
                    this.singleTypeCache = new Dictionary<Type, object>();
                }

                this.lastTypeCached = extensionType;
                this.lastObjectCached = extension;
                this.singleTypeCache[extensionType] = extension;
            }
        }

        private bool TryGetCachedExtension(Type type, out object extension)
        {
            if (this.singleTypeCache == null)
            {
                extension = null;
                return false;
            }

            if (object.ReferenceEquals(type, this.lastTypeCached))
            {
                extension = this.lastObjectCached;
                return true;
            }

            return this.singleTypeCache.TryGetValue(type, out extension);
        }
    }
}
