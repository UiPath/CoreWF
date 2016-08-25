// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.CoreWf.Runtime
{
    [DataContract]
    internal class ExecutionPropertyManager
    {
        private ActivityInstance _owningInstance;

        private Dictionary<string, ExecutionProperty> _properties;

        // Since the ExecutionProperty objects in this list
        // could exist in several places we need to make sure
        // that we clean up any booleans before more work items run
        private List<ExecutionProperty> _threadProperties;
        private bool _ownsThreadPropertiesList;

        private string _lastPropertyName;
        private object _lastProperty;
        private IdSpace _lastPropertyVisibility;

        // used by the root activity instance to chain parents correctly
        private ExecutionPropertyManager _rootPropertyManager;

        private int _exclusiveHandleCount;

        public ExecutionPropertyManager(ActivityInstance owningInstance)
        {
            Fx.Assert(owningInstance != null, "null instance should be using the internal host-based ctor");
            _owningInstance = owningInstance;

            // This object is only constructed if we know we have properties to add to it
            _properties = new Dictionary<string, ExecutionProperty>();

            if (owningInstance.HasChildren)
            {
                ActivityInstance previousOwner = owningInstance.PropertyManager != null ? owningInstance.PropertyManager._owningInstance : null;

                // we're setting a handle property. Walk the children and associate the new property manager
                // then walk our instance list, fixup parent references, and perform basic validation
                ActivityUtilities.ProcessActivityInstanceTree(owningInstance, null, (instance, executor) => AttachPropertyManager(instance, previousOwner));
            }
            else
            {
                owningInstance.PropertyManager = this;
            }
        }

        public ExecutionPropertyManager(ActivityInstance owningInstance, ExecutionPropertyManager parentPropertyManager)
            : this(owningInstance)
        {
            Fx.Assert(parentPropertyManager != null, "caller must verify");
            _threadProperties = parentPropertyManager._threadProperties;

            // if our parent is null, capture any root properties
            if (owningInstance.Parent == null)
            {
                _rootPropertyManager = parentPropertyManager._rootPropertyManager;
            }
        }

        internal ExecutionPropertyManager(ActivityInstance owningInstance, Dictionary<string, ExecutionProperty> properties)
        {
            Fx.Assert(properties != null, "properties should never be null");
            _owningInstance = owningInstance;
            _properties = properties;

            // owningInstance can be null (for host-provided root properties)
            if (owningInstance == null)
            {
                _rootPropertyManager = this;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "properties")]
        internal Dictionary<string, ExecutionProperty> SerializedProperties
        {
            get { return _properties; }
            set { _properties = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "exclusiveHandleCount")]
        internal int SerializedExclusiveHandleCount
        {
            get { return _exclusiveHandleCount; }
            set { _exclusiveHandleCount = value; }
        }

        internal Dictionary<string, ExecutionProperty> Properties
        {
            get
            {
                return _properties;
            }
        }

        internal bool HasExclusiveHandlesInScope
        {
            get
            {
                return _exclusiveHandleCount > 0;
            }
        }

        private bool AttachPropertyManager(ActivityInstance instance, ActivityInstance previousOwner)
        {
            if (instance.PropertyManager == null || instance.PropertyManager._owningInstance == previousOwner)
            {
                instance.PropertyManager = this;
                return true;
            }
            else
            {
                return false;
            }
        }

        public object GetProperty(string name, IdSpace currentIdSpace)
        {
            Fx.Assert(!string.IsNullOrEmpty(name), "The name should be validated by the caller.");

            if (_lastPropertyName == name && (_lastPropertyVisibility == null || _lastPropertyVisibility == currentIdSpace))
            {
                return _lastProperty;
            }

            ExecutionPropertyManager currentManager = this;

            while (currentManager != null)
            {
                ExecutionProperty property;
                if (currentManager._properties.TryGetValue(name, out property))
                {
                    if (!property.IsRemoved && (!property.HasRestrictedVisibility || property.Visibility == currentIdSpace))
                    {
                        _lastPropertyName = name;
                        _lastProperty = property.Property;
                        _lastPropertyVisibility = property.Visibility;

                        return _lastProperty;
                    }
                }

                currentManager = GetParent(currentManager);
            }

            return null;
        }

        private void AddProperties(IDictionary<string, ExecutionProperty> properties, IDictionary<string, object> flattenedProperties, IdSpace currentIdSpace)
        {
            foreach (KeyValuePair<string, ExecutionProperty> item in properties)
            {
                if (!item.Value.IsRemoved && !flattenedProperties.ContainsKey(item.Key) && (!item.Value.HasRestrictedVisibility || item.Value.Visibility == currentIdSpace))
                {
                    flattenedProperties.Add(item.Key, item.Value.Property);
                }
            }
        }

        public IEnumerable<KeyValuePair<string, object>> GetFlattenedProperties(IdSpace currentIdSpace)
        {
            ExecutionPropertyManager currentManager = this;
            Dictionary<string, object> flattenedProperties = new Dictionary<string, object>();
            while (currentManager != null)
            {
                AddProperties(currentManager.Properties, flattenedProperties, currentIdSpace);
                currentManager = GetParent(currentManager);
            }
            return flattenedProperties;
        }

        //Currently this is only used for the exclusive scope processing
        internal List<T> FindAll<T>() where T : class
        {
            ExecutionPropertyManager currentManager = this;
            List<T> list = null;

            while (currentManager != null)
            {
                foreach (ExecutionProperty property in currentManager.Properties.Values)
                {
                    if (property.Property is T)
                    {
                        if (list == null)
                        {
                            list = new List<T>();
                        }
                        list.Add((T)property.Property);
                    }
                }

                currentManager = GetParent(currentManager);
            }

            return list;
        }

        private static ExecutionPropertyManager GetParent(ExecutionPropertyManager currentManager)
        {
            if (currentManager._owningInstance != null)
            {
                if (currentManager._owningInstance.Parent != null)
                {
                    return currentManager._owningInstance.Parent.PropertyManager;
                }
                else
                {
                    return currentManager._rootPropertyManager;
                }
            }
            else
            {
                return null;
            }
        }

        public void Add(string name, object property, IdSpace visibility)
        {
            Fx.Assert(!string.IsNullOrEmpty(name), "The name should be validated before calling this collection.");
            Fx.Assert(property != null, "The property should be validated before caling this collection.");

            ExecutionProperty executionProperty = new ExecutionProperty(name, property, visibility);
            _properties.Add(name, executionProperty);

            if (_lastPropertyName == name)
            {
                _lastProperty = property;
            }

            if (property is ExclusiveHandle)
            {
                _exclusiveHandleCount++;

                UpdateChildExclusiveHandleCounts(1);
            }

            if (property is IExecutionProperty)
            {
                AddIExecutionProperty(executionProperty, false);
            }
        }

        private void UpdateChildExclusiveHandleCounts(int amountToUpdate)
        {
            Queue<HybridCollection<ActivityInstance>> toProcess = null;

            HybridCollection<ActivityInstance> children = _owningInstance.GetRawChildren();

            if (children != null && children.Count > 0)
            {
                ProcessChildrenForExclusiveHandles(children, amountToUpdate, ref toProcess);

                if (toProcess != null)
                {
                    while (toProcess.Count > 0)
                    {
                        children = toProcess.Dequeue();
                        ProcessChildrenForExclusiveHandles(children, amountToUpdate, ref toProcess);
                    }
                }
            }
        }

        private void ProcessChildrenForExclusiveHandles(HybridCollection<ActivityInstance> children, int amountToUpdate, ref Queue<HybridCollection<ActivityInstance>> toProcess)
        {
            for (int i = 0; i < children.Count; i++)
            {
                ActivityInstance child = children[i];

                ExecutionPropertyManager childManager = child.PropertyManager;

                if (childManager.IsOwner(child))
                {
                    childManager._exclusiveHandleCount += amountToUpdate;
                }

                HybridCollection<ActivityInstance> tempChildren = child.GetRawChildren();

                if (tempChildren != null && tempChildren.Count > 0)
                {
                    if (toProcess == null)
                    {
                        toProcess = new Queue<HybridCollection<ActivityInstance>>();
                    }

                    toProcess.Enqueue(tempChildren);
                }
            }
        }

        private void AddIExecutionProperty(ExecutionProperty property, bool isDeserializationFixup)
        {
            bool willCleanupBeCalled = !isDeserializationFixup;

            if (_threadProperties == null)
            {
                _threadProperties = new List<ExecutionProperty>(1);
                _ownsThreadPropertiesList = true;
            }
            else if (!_ownsThreadPropertiesList)
            {
                List<ExecutionProperty> updatedProperties = new List<ExecutionProperty>(_threadProperties.Count);

                // We need to copy all properties to our new list and we
                // need to mark hidden properties as "to be removed" (or just
                // not copy them on the deserialization path)
                for (int i = 0; i < _threadProperties.Count; i++)
                {
                    ExecutionProperty currentProperty = _threadProperties[i];

                    if (currentProperty.Name == property.Name)
                    {
                        if (willCleanupBeCalled)
                        {
                            currentProperty.ShouldBeRemovedAfterCleanup = true;
                            updatedProperties.Add(currentProperty);
                        }

                        // If cleanup won't be called then we are on the
                        // deserialization path and shouldn't copy this
                        // property over to our new list
                    }
                    else
                    {
                        updatedProperties.Add(currentProperty);
                    }
                }

                _threadProperties = updatedProperties;
                _ownsThreadPropertiesList = true;
            }
            else
            {
                for (int i = _threadProperties.Count - 1; i >= 0; i--)
                {
                    ExecutionProperty currentProperty = _threadProperties[i];

                    if (currentProperty.Name == property.Name)
                    {
                        if (willCleanupBeCalled)
                        {
                            currentProperty.ShouldBeRemovedAfterCleanup = true;
                        }
                        else
                        {
                            _threadProperties.RemoveAt(i);
                        }

                        // There will only be at most one property in this list that
                        // matches the name
                        break;
                    }
                }
            }

            property.ShouldSkipNextCleanup = willCleanupBeCalled;
            _threadProperties.Add(property);
        }

        public void Remove(string name)
        {
            Fx.Assert(!string.IsNullOrEmpty(name), "This should have been validated by the caller.");

            ExecutionProperty executionProperty = _properties[name];

            Fx.Assert(executionProperty != null, "This should only be called if we know the property exists");

            if (executionProperty.Property is IExecutionProperty)
            {
                Fx.Assert(_ownsThreadPropertiesList && _threadProperties != null, "We should definitely be the list owner if we have an IExecutionProperty");

                if (!_threadProperties.Remove(executionProperty))
                {
                    Fx.Assert("We should have had this property in the list.");
                }
            }

            _properties.Remove(name);

            if (executionProperty.Property is ExclusiveHandle)
            {
                _exclusiveHandleCount--;

                UpdateChildExclusiveHandleCounts(-1);
            }

            if (_lastPropertyName == name)
            {
                _lastPropertyName = null;
                _lastProperty = null;
            }
        }

        public object GetPropertyAtCurrentScope(string name)
        {
            Fx.Assert(!string.IsNullOrEmpty(name), "This should be validated elsewhere");

            ExecutionProperty property;
            if (_properties.TryGetValue(name, out property))
            {
                return property.Property;
            }

            return null;
        }

        public bool IsOwner(ActivityInstance instance)
        {
            return _owningInstance == instance;
        }

        //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode, //Justification = "Called from Serialization")]
        internal bool ShouldSerialize(ActivityInstance instance)
        {
            return IsOwner(instance) && _properties.Count > 0;
        }

        public void SetupWorkflowThread()
        {
            if (_threadProperties != null)
            {
                for (int i = 0; i < _threadProperties.Count; i++)
                {
                    ExecutionProperty executionProperty = _threadProperties[i];
                    executionProperty.ShouldSkipNextCleanup = false;
                    IExecutionProperty property = (IExecutionProperty)executionProperty.Property;

                    property.SetupWorkflowThread();
                }
            }
        }

        // This method only throws fatal exceptions
        public void CleanupWorkflowThread(ref Exception abortException)
        {
            if (_threadProperties != null)
            {
                for (int i = _threadProperties.Count - 1; i >= 0; i--)
                {
                    ExecutionProperty current = _threadProperties[i];

                    if (current.ShouldSkipNextCleanup)
                    {
                        current.ShouldSkipNextCleanup = false;
                    }
                    else
                    {
                        IExecutionProperty property = (IExecutionProperty)current.Property;

                        try
                        {
                            property.CleanupWorkflowThread();
                        }
                        catch (Exception e)
                        {
                            if (Fx.IsFatal(e))
                            {
                                throw;
                            }

                            abortException = e;
                        }
                    }

                    if (current.ShouldBeRemovedAfterCleanup)
                    {
                        _threadProperties.RemoveAt(i);
                        current.ShouldBeRemovedAfterCleanup = false;
                    }
                }
            }
        }

        public void UnregisterProperties(ActivityInstance completedInstance, IdSpace currentIdSpace)
        {
            UnregisterProperties(completedInstance, currentIdSpace, false);
        }

        public void UnregisterProperties(ActivityInstance completedInstance, IdSpace currentIdSpace, bool ignoreExceptions)
        {
            if (IsOwner(completedInstance))
            {
                RegistrationContext registrationContext = new RegistrationContext(this, currentIdSpace);

                foreach (ExecutionProperty property in _properties.Values)
                {
                    // We do a soft removal because we're about to throw away this dictionary
                    // and we don't want to mess up our enumerator
                    property.IsRemoved = true;

                    IPropertyRegistrationCallback registrationCallback = property.Property as IPropertyRegistrationCallback;

                    if (registrationCallback != null)
                    {
                        try
                        {
                            registrationCallback.Unregister(registrationContext);
                        }
                        catch (Exception e)
                        {
                            if (Fx.IsFatal(e) || !ignoreExceptions)
                            {
                                throw;
                            }
                        }
                    }
                }

                Fx.Assert(completedInstance == null || completedInstance.GetRawChildren() == null || completedInstance.GetRawChildren().Count == 0, "There must not be any children at this point otherwise our exclusive handle count would be incorrect.");

                // We still need to clear this list in case any non-serializable
                // properties were being used in a no persist zone
                _properties.Clear();
            }
        }

        public void ThrowIfAlreadyDefined(string name, ActivityInstance executingInstance)
        {
            if (executingInstance == _owningInstance)
            {
                if (_properties.ContainsKey(name))
                {
                    throw Microsoft.CoreWf.Internals.FxTrace.Exception.Argument("name", SR.ExecutionPropertyAlreadyDefined(name));
                }
            }
        }

        public void OnDeserialized(ActivityInstance owner, ActivityInstance parent, IdSpace visibility, ActivityExecutor executor)
        {
            _owningInstance = owner;

            if (parent != null)
            {
                if (parent.PropertyManager != null)
                {
                    _threadProperties = parent.PropertyManager._threadProperties;
                }
            }
            else
            {
                _rootPropertyManager = executor.RootPropertyManager;
            }

            foreach (ExecutionProperty property in _properties.Values)
            {
                if (property.Property is IExecutionProperty)
                {
                    AddIExecutionProperty(property, true);
                }

                if (property.HasRestrictedVisibility)
                {
                    property.Visibility = visibility;
                }
            }
        }

        [DataContract]
        internal class ExecutionProperty
        {
            private string _name;
            private object _property;
            private bool _hasRestrictedVisibility;

            public ExecutionProperty(string name, object property, IdSpace visibility)
            {
                this.Name = name;
                this.Property = property;

                if (visibility != null)
                {
                    this.Visibility = visibility;
                    this.HasRestrictedVisibility = true;
                }
            }

            public string Name
            {
                get
                {
                    return _name;
                }
                private set
                {
                    _name = value;
                }
            }

            public object Property
            {
                get
                {
                    return _property;
                }
                private set
                {
                    _property = value;
                }
            }

            public bool HasRestrictedVisibility
            {
                get
                {
                    return _hasRestrictedVisibility;
                }
                private set
                {
                    _hasRestrictedVisibility = value;
                }
            }

            // This property is fixed up at deserialization time
            public IdSpace Visibility
            {
                get;
                set;
            }

            // This is always false at persistence because
            // a removed property belongs to an activity which
            // has completed and is therefore not part of the 
            // instance map anymore
            public bool IsRemoved { get; set; }

            // These don't need to be serialized because they are only
            // ever false at persistence time.  We potentially set
            // them to true when a property is added but we always
            // reset them to false after cleaning up the thread
            public bool ShouldBeRemovedAfterCleanup { get; set; }
            public bool ShouldSkipNextCleanup { get; set; }

            [DataMember(Name = "Name")]
            internal string SerializedName
            {
                get { return this.Name; }
                set { this.Name = value; }
            }

            [DataMember(Name = "Property")]
            internal object SerializedProperty
            {
                get { return this.Property; }
                set { this.Property = value; }
            }

            [DataMember(EmitDefaultValue = false, Name = "HasRestrictedVisibility")]
            internal bool SerializedHasRestrictedVisibility
            {
                get { return this.HasRestrictedVisibility; }
                set { this.HasRestrictedVisibility = value; }
            }
        }
    }
}


