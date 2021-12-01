// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;
using System.Activities.Tracking;
using System.Linq;

namespace System.Activities.Hosting;

// One workflow host should have one manager, and one manager should have one catalog.
// One workflow instance should have one container as the instance itself would be
// added as one extension to the container as well
public class WorkflowInstanceExtensionManager
{
    // using an empty list instead of null simplifies our calculations immensely
    internal static List<KeyValuePair<Type, WorkflowInstanceExtensionProvider>> EmptyExtensionProviders = new(0);
    internal static List<object> EmptySingletonExtensions = new(0);
    private bool _isReadonly;
    private List<object> _additionalSingletonExtensions;
    private List<object> _allSingletonExtensions;
    private bool _hasSingletonTrackingParticipant;
    private bool _hasSingletonPersistenceModule;

    public WorkflowInstanceExtensionManager() { }

    internal SymbolResolver SymbolResolver { get; private set; }

    internal List<object> SingletonExtensions { get; private set; }

    internal List<object> AdditionalSingletonExtensions => _additionalSingletonExtensions;

    internal List<KeyValuePair<Type, WorkflowInstanceExtensionProvider>> ExtensionProviders { get; private set; }

    internal bool HasSingletonIWorkflowInstanceExtensions { get; private set; }

    internal bool HasSingletonTrackingParticipant => _hasSingletonTrackingParticipant;

    internal bool HasSingletonPersistenceModule => _hasSingletonPersistenceModule;

    internal bool HasAdditionalSingletonIWorkflowInstanceExtensions { get; private set; }

    // use this method to add the singleton extension
    public virtual void Add(object singletonExtension)
    {
        if (singletonExtension == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(singletonExtension));
        }

        ThrowIfReadOnly();

        if (singletonExtension is SymbolResolver resolver)
        {
            if (SymbolResolver != null)
            {
                throw FxTrace.Exception.Argument(nameof(singletonExtension), SR.SymbolResolverAlreadyExists);
            }
            SymbolResolver = resolver;
        }
        else
        {
            if (singletonExtension is IWorkflowInstanceExtension)
            {
                HasSingletonIWorkflowInstanceExtensions = true;
            }
            if (!HasSingletonTrackingParticipant && singletonExtension is TrackingParticipant)
            {
                _hasSingletonTrackingParticipant = true;
            }
            if (!HasSingletonPersistenceModule && singletonExtension is IPersistencePipelineModule)
            {
                _hasSingletonPersistenceModule = true;
            }
        }

        SingletonExtensions ??= new List<object>();
        SingletonExtensions.Add(singletonExtension);
    }

    // use this method to add a per-instance extension
    public virtual void Add<T>(Func<T> extensionCreationFunction) where T : class
    {
        if (extensionCreationFunction == null)
        {
            throw FxTrace.Exception.ArgumentNull(nameof(extensionCreationFunction));
        }
        ThrowIfReadOnly();

        ExtensionProviders ??= new List<KeyValuePair<Type, WorkflowInstanceExtensionProvider>>();
        ExtensionProviders.Add(new KeyValuePair<Type, WorkflowInstanceExtensionProvider>(typeof(T), new WorkflowInstanceExtensionProvider<T>(extensionCreationFunction)));
    }

    internal List<object> GetAllSingletonExtensions() => _allSingletonExtensions;

    internal void AddAllExtensionTypes(HashSet<Type> extensionTypes)
    {
        Fx.Assert(_isReadonly, "should be read only at this point");
        for (int i = 0; i < SingletonExtensions.Count; i++)
        {
            extensionTypes.Add(SingletonExtensions[i].GetType());
        }
        for (int i = 0; i < ExtensionProviders.Count; i++)
        {
            extensionTypes.Add(ExtensionProviders[i].Key);
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
        if (newExtension is not IWorkflowInstanceExtension currentInstanceExtension)
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
                    if (additionalExtension is IWorkflowInstanceExtension extension)
                    {
                        additionalInstanceExtensions ??= new Queue<IWorkflowInstanceExtension>();
                        additionalInstanceExtensions.Enqueue(extension);
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
            if (SingletonExtensions != null)
            {
                if (HasSingletonIWorkflowInstanceExtensions)
                {
                    foreach (IWorkflowInstanceExtension additionalExtensionProvider in SingletonExtensions.OfType<IWorkflowInstanceExtension>())
                    {
                        AddExtensionClosure(additionalExtensionProvider, ref _additionalSingletonExtensions, ref _hasSingletonTrackingParticipant, ref _hasSingletonPersistenceModule);
                    }

                    if (AdditionalSingletonExtensions != null)
                    {
                        for (int i = 0; i < AdditionalSingletonExtensions.Count; i++)
                        {
                            object extension = AdditionalSingletonExtensions[i];
                            if (extension is IWorkflowInstanceExtension)
                            {
                                HasAdditionalSingletonIWorkflowInstanceExtensions = true;
                                break;
                            }
                        }
                    }
                }

                _allSingletonExtensions = SingletonExtensions;
                if (AdditionalSingletonExtensions != null && AdditionalSingletonExtensions.Count > 0)
                {
                    _allSingletonExtensions = new List<object>(SingletonExtensions);
                    _allSingletonExtensions.AddRange(AdditionalSingletonExtensions);
                }
            }
            else
            {
                SingletonExtensions = EmptySingletonExtensions;
                _allSingletonExtensions = EmptySingletonExtensions;
            }

            ExtensionProviders ??= EmptyExtensionProviders;
            _isReadonly = true;
        }
    }

    private void ThrowIfReadOnly()
    {
        if (_isReadonly)
        {
            throw FxTrace.Exception.AsError(new InvalidOperationException(SR.ExtensionsCannotBeModified));
        }
    }
}
