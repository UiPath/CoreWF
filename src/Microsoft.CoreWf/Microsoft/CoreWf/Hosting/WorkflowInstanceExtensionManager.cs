// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CoreWf.Runtime;
using Microsoft.CoreWf.Tracking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CoreWf.Hosting
{
    // One workflow host should have one manager, and one manager should have one catalog.
    // One workflow instance should have one container as the instance itself would be
    // added as one extension to the container as well
    public class WorkflowInstanceExtensionManager
    {
        // using an empty list instead of null simplifies our calculations immensely
        internal static List<KeyValuePair<Type, WorkflowInstanceExtensionProvider>> EmptyExtensionProviders = new List<KeyValuePair<Type, WorkflowInstanceExtensionProvider>>(0);
        internal static List<object> EmptySingletonExtensions = new List<object>(0);

        private bool _isReadonly;
        private List<object> _additionalSingletonExtensions;
        private List<object> _allSingletonExtensions;

        private bool _hasSingletonTrackingParticipant;
        private bool _hasSingletonPersistenceModule;

        public WorkflowInstanceExtensionManager()
        {
        }

        internal SymbolResolver SymbolResolver
        {
            get;
            private set;
        }

        internal List<object> SingletonExtensions
        {
            get;
            private set;
        }

        internal List<object> AdditionalSingletonExtensions
        {
            get
            {
                return _additionalSingletonExtensions;
            }
        }

        internal List<KeyValuePair<Type, WorkflowInstanceExtensionProvider>> ExtensionProviders
        {
            get;
            private set;
        }

        internal bool HasSingletonIWorkflowInstanceExtensions
        {
            get;
            private set;
        }

        internal bool HasSingletonTrackingParticipant
        {
            get
            {
                return _hasSingletonTrackingParticipant;
            }
        }

        internal bool HasSingletonPersistenceModule
        {
            get
            {
                return _hasSingletonPersistenceModule;
            }
        }

        internal bool HasAdditionalSingletonIWorkflowInstanceExtensions
        {
            get;
            private set;
        }

        // use this method to add the singleton extension
        public virtual void Add(object singletonExtension)
        {
            if (singletonExtension == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("singletonExtension");
            }

            ThrowIfReadOnly();

            if (singletonExtension is SymbolResolver)
            {
                if (this.SymbolResolver != null)
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("singletonExtension", SR.SymbolResolverAlreadyExists);
                }
                this.SymbolResolver = (SymbolResolver)singletonExtension;
            }
            else
            {
                if (singletonExtension is IWorkflowInstanceExtension)
                {
                    HasSingletonIWorkflowInstanceExtensions = true;
                }
                if (!this.HasSingletonTrackingParticipant && singletonExtension is TrackingParticipant)
                {
                    _hasSingletonTrackingParticipant = true;
                }
                if (!this.HasSingletonPersistenceModule && singletonExtension is IPersistencePipelineModule)
                {
                    _hasSingletonPersistenceModule = true;
                }
            }

            if (this.SingletonExtensions == null)
            {
                this.SingletonExtensions = new List<object>();
            }

            this.SingletonExtensions.Add(singletonExtension);
        }

        // use this method to add a per-instance extension
        public virtual void Add<T>(Func<T> extensionCreationFunction) where T : class
        {
            if (extensionCreationFunction == null)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.ArgumentNull("extensionCreationFunction");
            }
            ThrowIfReadOnly();

            if (this.ExtensionProviders == null)
            {
                this.ExtensionProviders = new List<KeyValuePair<Type, WorkflowInstanceExtensionProvider>>();
            }

            this.ExtensionProviders.Add(new KeyValuePair<Type, WorkflowInstanceExtensionProvider>(typeof(T), new WorkflowInstanceExtensionProvider<T>(extensionCreationFunction)));
        }

        internal List<object> GetAllSingletonExtensions()
        {
            return _allSingletonExtensions;
        }

        internal void AddAllExtensionTypes(HashSet<Type> extensionTypes)
        {
            Fx.Assert(_isReadonly, "should be read only at this point");
            for (int i = 0; i < this.SingletonExtensions.Count; i++)
            {
                extensionTypes.Add(this.SingletonExtensions[i].GetType());
            }
            for (int i = 0; i < this.ExtensionProviders.Count; i++)
            {
                extensionTypes.Add(this.ExtensionProviders[i].Key);
            }
        }

        internal static WorkflowInstanceExtensionCollection CreateInstanceExtensions(Activity workflowDefinition, WorkflowInstanceExtensionManager extensionManager)
        {
            Fx.Assert(workflowDefinition.IsRuntimeReady, "activity should be ready with extensions after a successful CacheMetadata call");
            if (extensionManager != null)
            {
                extensionManager.MakeReadOnly();
                return new WorkflowInstanceExtensionCollection(workflowDefinition, extensionManager);
            }
            else if ((workflowDefinition.DefaultExtensionsCount > 0) || (workflowDefinition.RequiredExtensionTypesCount > 0))
            {
                return new WorkflowInstanceExtensionCollection(workflowDefinition, null);
            }
            else
            {
                return null;
            }
        }

        internal static void AddExtensionClosure(object newExtension, ref List<object> targetCollection, ref bool addedTrackingParticipant, ref bool addedPersistenceModule)
        {
            // see if we need to process "additional" extensions
            IWorkflowInstanceExtension currentInstanceExtension = newExtension as IWorkflowInstanceExtension;
            if (currentInstanceExtension == null)
            {
                return; // bail early
            }

            Queue<IWorkflowInstanceExtension> additionalInstanceExtensions = null;
            if (targetCollection == null)
            {
                targetCollection = new List<object>();
            }

            while (currentInstanceExtension != null)
            {
                IEnumerable<object> additionalExtensions = currentInstanceExtension.GetAdditionalExtensions();
                if (additionalExtensions != null)
                {
                    foreach (object additionalExtension in additionalExtensions)
                    {
                        targetCollection.Add(additionalExtension);
                        if (additionalExtension is IWorkflowInstanceExtension)
                        {
                            if (additionalInstanceExtensions == null)
                            {
                                additionalInstanceExtensions = new Queue<IWorkflowInstanceExtension>();
                            }
                            additionalInstanceExtensions.Enqueue((IWorkflowInstanceExtension)additionalExtension);
                        }
                        if (!addedTrackingParticipant && additionalExtension is TrackingParticipant)
                        {
                            addedTrackingParticipant = true;
                        }
                        if (!addedPersistenceModule && additionalExtension is IPersistencePipelineModule)
                        {
                            addedPersistenceModule = true;
                        }
                    }
                }

                if (additionalInstanceExtensions != null && additionalInstanceExtensions.Count > 0)
                {
                    currentInstanceExtension = additionalInstanceExtensions.Dequeue();
                }
                else
                {
                    currentInstanceExtension = null;
                }
            }
        }

        public void MakeReadOnly()
        {
            // if any singleton extensions have dependents, calculate them now so that we're only
            // doing this process once per-host
            if (!_isReadonly)
            {
                if (this.SingletonExtensions != null)
                {
                    if (HasSingletonIWorkflowInstanceExtensions)
                    {
                        foreach (IWorkflowInstanceExtension additionalExtensionProvider in this.SingletonExtensions.OfType<IWorkflowInstanceExtension>())
                        {
                            AddExtensionClosure(additionalExtensionProvider, ref _additionalSingletonExtensions, ref _hasSingletonTrackingParticipant, ref _hasSingletonPersistenceModule);
                        }

                        if (this.AdditionalSingletonExtensions != null)
                        {
                            for (int i = 0; i < this.AdditionalSingletonExtensions.Count; i++)
                            {
                                object extension = this.AdditionalSingletonExtensions[i];
                                if (extension is IWorkflowInstanceExtension)
                                {
                                    HasAdditionalSingletonIWorkflowInstanceExtensions = true;
                                    break;
                                }
                            }
                        }
                    }

                    _allSingletonExtensions = this.SingletonExtensions;
                    if (this.AdditionalSingletonExtensions != null && this.AdditionalSingletonExtensions.Count > 0)
                    {
                        _allSingletonExtensions = new List<object>(this.SingletonExtensions);
                        _allSingletonExtensions.AddRange(this.AdditionalSingletonExtensions);
                    }
                }
                else
                {
                    this.SingletonExtensions = EmptySingletonExtensions;
                    _allSingletonExtensions = EmptySingletonExtensions;
                }

                if (this.ExtensionProviders == null)
                {
                    this.ExtensionProviders = EmptyExtensionProviders;
                }

                _isReadonly = true;
            }
        }

        private void ThrowIfReadOnly()
        {
            if (_isReadonly)
            {
                throw Microsoft.CoreWf.Internals.FxTrace.Exception.AsError(new InvalidOperationException(SR.ExtensionsCannotBeModified));
            }
        }
    }
}
