// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace System.Activities
{
    using System;
    using System.Activities.Runtime;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Activities.Internals;

    [Fx.Tag.XamlVisible(false)]
    public sealed class WorkflowDataContext : CustomTypeDescriptor, INotifyPropertyChanged, IDisposable
    {
        private readonly ActivityExecutor executor;
        private ActivityInstance activityInstance;
        private IDictionary<Location, PropertyDescriptorImpl> locationMapping;
        private PropertyChangedEventHandler propertyChangedEventHandler;
        private readonly PropertyDescriptorCollection properties;
        private ActivityContext cachedResolutionContext;

        internal WorkflowDataContext(ActivityExecutor executor, ActivityInstance activityInstance, bool includeLocalVariables)
        {
            this.executor = executor;
            this.activityInstance = activityInstance;
            this.IncludesLocalVariables = includeLocalVariables;
            this.properties = CreateProperties();
        }

        internal bool IncludesLocalVariables
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // We want our own cached ActivityContext rather than using this.executor.GetResolutionContext
        // because there is no synchronization of access to the executor's cached object and access thru
        // this WorkflowDataContext will not be done on the workflow runtime thread.
        private ActivityContext ResolutionContext
        {
            get
            {
                ThrowIfEnvironmentDisposed();
                if (this.cachedResolutionContext == null)
                {
                    this.cachedResolutionContext = new ActivityContext(this.activityInstance, this.executor)
                    {
                        AllowChainedEnvironmentAccess = true
                    };
                }
                else
                {
                    this.cachedResolutionContext.Reinitialize(this.activityInstance, this.executor);
                }
                return this.cachedResolutionContext;
            }
        }

        private PropertyChangedEventHandler PropertyChangedEventHandler
        {
            get
            {
                if (this.propertyChangedEventHandler == null)
                {
                    this.propertyChangedEventHandler = new PropertyChangedEventHandler(this.OnLocationChanged);
                }
                return this.propertyChangedEventHandler;
            }
        }

        private PropertyDescriptorCollection CreateProperties()
        {
            // The name in child Activity will shadow the name in parent.
            Dictionary<string, object> names = new Dictionary<string, object>();

            List<PropertyDescriptorImpl> propertyList = new List<PropertyDescriptorImpl>();

            LocationReferenceEnvironment environment = this.activityInstance.Activity.PublicEnvironment;
            bool isLocalEnvironment = true;
            while (environment != null)
            {
                foreach (LocationReference locRef in environment.GetLocationReferences())
                {
                    if (this.IncludesLocalVariables || !isLocalEnvironment || !(locRef is Variable))
                    {
                        AddProperty(locRef, names, propertyList);
                    }
                }

                environment = environment.Parent;
                isLocalEnvironment = false;
            }

            return new PropertyDescriptorCollection(propertyList.ToArray(), true);
        }

        private void AddProperty(LocationReference reference, Dictionary<string, object> names,
            List<PropertyDescriptorImpl> propertyList)
        {
            if (!string.IsNullOrEmpty(reference.Name) &&
                !names.ContainsKey(reference.Name))
            {
                names.Add(reference.Name, reference);
                PropertyDescriptorImpl property = new PropertyDescriptorImpl(reference);
                propertyList.Add(property);
                AddNotifyHandler(property);
            }
        }

        private void AddNotifyHandler(PropertyDescriptorImpl property)
        {
            ActivityContext activityContext = this.ResolutionContext;
            try
            {
                Location location = property.LocationReference.GetLocation(activityContext);
                if (location is INotifyPropertyChanged notify)
                {
                    notify.PropertyChanged += PropertyChangedEventHandler;

                    if (this.locationMapping == null)
                    {
                        this.locationMapping = new Dictionary<Location, PropertyDescriptorImpl>();
                    }
                    this.locationMapping.Add(location, property);
                }
            }
            finally
            {
                activityContext.Dispose();
            }
        }

        private void OnLocationChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                Location location = (Location)sender;

                Fx.Assert(this.locationMapping != null, "Location mapping must not be null.");
                if (this.locationMapping.TryGetValue(location, out PropertyDescriptorImpl property))
                {
                    if (e.PropertyName == "Value")
                    {
                        handler(this, new PropertyChangedEventArgs(property.Name));
                    }
                    else
                    {
                        handler(this, new PropertyChangedEventArgs(property.Name + "." + e.PropertyName));
                    }
                }
            }
        }

        public void Dispose()
        {
            if (this.locationMapping != null)
            {
                foreach (KeyValuePair<Location, PropertyDescriptorImpl> pair in this.locationMapping)
                {
                    if (pair.Key is INotifyPropertyChanged notify)
                    {
                        notify.PropertyChanged -= PropertyChangedEventHandler;
                    }
                }
            }
        }

        // We need a separate method here from Dispose(), because Dispose currently
        // doesn't make the WDC uncallable, it just unhooks it from notifications.
        internal void DisposeEnvironment()
        {
            this.activityInstance = null;
        }

        private void ThrowIfEnvironmentDisposed()
        {
            if (this.activityInstance == null)
            {
                throw FxTrace.Exception.AsError(
                    new ObjectDisposedException(this.GetType().FullName, SR.WDCDisposed));
            }
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return this.properties;
        }

        private class PropertyDescriptorImpl : PropertyDescriptor
        {
            private readonly LocationReference reference;
            // TODO 131998, We should support readonly LocationReferences.
            // bool isReadOnly;


            public PropertyDescriptorImpl(LocationReference reference)
                : base(reference.Name, new Attribute[0])
            {
                this.reference = reference;
            }

            public override Type ComponentType
            {
                get { return typeof(WorkflowDataContext); }
            }

            public override bool IsReadOnly
            {
                get
                {
                    // TODO 131998, We should support readonly LocationReferences.
                    // return this.isReadOnly;
                    return false;
                }
            }

            public override Type PropertyType
            {
                get
                {
                    return this.reference.Type;
                }
            }

            public LocationReference LocationReference
            {
                get
                {
                    return this.reference;
                }
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override object GetValue(object component)
            {
                WorkflowDataContext dataContext = (WorkflowDataContext)component;

                ActivityContext activityContext = dataContext.ResolutionContext;
                try
                {
                    return this.reference.GetLocation(activityContext).Value;
                }
                finally
                {
                    activityContext.Dispose();
                }
            }

            public override void ResetValue(object component)
            {
                throw FxTrace.Exception.AsError(new NotSupportedException(SR.CannotResetPropertyInDataContext));
            }

            public override void SetValue(object component, object value)
            {
                if (IsReadOnly)
                {
                    throw FxTrace.Exception.AsError(new NotSupportedException(SR.PropertyReadOnlyInWorkflowDataContext(this.Name)));
                }

                WorkflowDataContext dataContext = (WorkflowDataContext)component;

                ActivityContext activityContext = dataContext.ResolutionContext;
                try
                {
                    Location location = this.reference.GetLocation(activityContext);
                    location.Value = value;
                }
                finally
                {
                    activityContext.Dispose();
                }
            }

            public override bool ShouldSerializeValue(object component)
            {
                return true;
            }
        }
    }
}
