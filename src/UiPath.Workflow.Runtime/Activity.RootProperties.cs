// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities;
using Hosting;
using Validation;

public abstract partial class Activity
{
    // information used by root activities
    private class RootProperties
    {
        private Dictionary<string, Activity> _singletonActivityNames;
        private Dictionary<Type, WorkflowInstanceExtensionProvider> _activityExtensionProviders;
        private HashSet<Type> _requiredExtensionTypes;

        public RootProperties() { }

        public bool HasBeenAssociatedWithAnInstance { get; set; }

        public LocationReferenceEnvironment HostEnvironment { get; set; }

        public Dictionary<string, List<RuntimeArgument>> OverloadGroups { get; set; }

        public List<RuntimeArgument> RequiredArgumentsNotInOverloadGroups { get; set; }

        public ValidationHelper.OverloadGroupEquivalenceInfo EquivalenceInfo { get; set; }

        public int DefaultExtensionsCount => _activityExtensionProviders != null ? _activityExtensionProviders.Count : 0;

        public int RequiredExtensionTypesCount => _requiredExtensionTypes != null ? _requiredExtensionTypes.Count : 0;

        public bool GetActivityExtensionInformation(out Dictionary<Type, WorkflowInstanceExtensionProvider> activityExtensionProviders, out HashSet<Type> requiredActivityExtensionTypes)
        {
            activityExtensionProviders = _activityExtensionProviders;
            requiredActivityExtensionTypes = _requiredExtensionTypes;
            return activityExtensionProviders != null || (_requiredExtensionTypes != null && _requiredExtensionTypes.Count > 0);
        }

        public void AddDefaultExtensionProvider<T>(Func<T> extensionProvider)
            where T : class
        {
            Type key = typeof(T);
            if (_activityExtensionProviders == null)
            {
                _activityExtensionProviders = new Dictionary<Type, WorkflowInstanceExtensionProvider>();
            }
            else
            {
                if (_activityExtensionProviders.ContainsKey(key))
                {
                    return; // already have a provider of this type
                }
            }

            _activityExtensionProviders.Add(key, new WorkflowInstanceExtensionProvider<T>(extensionProvider));

            // if we're providing an extension that exactly matches a required type, simplify further bookkeeping
            if (_requiredExtensionTypes != null)
            {
                _requiredExtensionTypes.Remove(key);
            }
        }

        public void RequireExtension(Type extensionType)
        {
            // if we're providing an extension that exactly matches a required type, don't bother with further bookkeeping
            if (_activityExtensionProviders != null && _activityExtensionProviders.ContainsKey(extensionType))
            {
                return;
            }

            if (_requiredExtensionTypes == null)
            {
                _requiredExtensionTypes = new HashSet<Type>();
            }
            _requiredExtensionTypes.Add(extensionType);
        }

        public bool IsSingletonActivityDeclared(string name) => _singletonActivityNames != null && _singletonActivityNames.ContainsKey(name);

        public void DeclareSingletonActivity(string name, Activity activity)
        {
            _singletonActivityNames ??= new Dictionary<string, Activity>(1);
            _singletonActivityNames.Add(name, activity);
        }

        public Activity GetSingletonActivity(string name)
        {
            Activity result = null;
            _singletonActivityNames?.TryGetValue(name, out result);
            return result;
        }
    }
}
